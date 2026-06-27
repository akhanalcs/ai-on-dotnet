# 🧠 Lesson: Fixing a Flag When Its Meaning Changes (DST Example)

## 🧩 Problem

We had a column:

```
DST_HR_ID
```

Originally, it was used as a **duplicate disambiguator**, not a true DST indicator.

### Example: Fall Back (Nov 2, 2025)

| Hour Ending | DST_HR_ID | Meaning                      |
|------------|-------------|------------------------------|
| 2          | 0           | First 1 AM (DST)             |
| 2          | 1           | Second 1 AM (Standard)       |

**Note:** Only the repeated hour gets DST_HR_ID = 1. 
* `DST_HOUR_ID = 0` -> everywhere
* `DST_HOUR_ID = 1` -> only for duplicated hour on fall back day

## 🔄 Requirement Change

### Example: Fall Back (Nov 2, 2025) with New Meaning

| Hour Ending | DST_HR_ID | Meaning                         |
|------------|-----------|---------------------------------|
| 2          | 1         | First 1 AM (DST / EDT)          |
| 2          | 0         | Second 1 AM (Standard / EST)    |

**Key Idea:**  
The same two rows still exist, but now the value directly represents  
whether that hour is in DST (1) or Standard Time (0).

***

# 🕒 How Hour Ending (HE) Works

Each row uses **Hour Ending (HE)**:

```
HE1  → 12:00 AM – 1:00 AM
HE2  → 1:00 AM – 2:00 AM
HE3  → 2:00 AM – 3:00 AM
HE4  → 3:00 AM – 4:00 AM
...
HE24 → 11:00 PM – 12:00 AM
```

✅ Key idea:

> **HE = the hour that just ended**

***

# ⏱ DST Transition Effects

## 🌸 Spring Forward

* 2:00 AM → 3:00 AM  
  👉 The **2:00–3:00 AM hour does NOT exist**

```
HE3 → missing ❌
```

***

## 🍂 Fall Back

* 2:00 AM → 1:00 AM  
  👉 The **1:00–2:00 AM hour happens twice**

```
HE2 → duplicated ✅
```

***

# 📊 Example: Existing vs To be Data

This makes the problem immediately obvious.

***

✅ Legend:

* ❌ = incorrect under new DST rules
* ✅ = corrected value

***

## ❌ Existing (before fix)

### 🌸 09‑MAR‑2025

| EVENT\_DATE | HOUR\_ENDING | DST\_HR\_ID |
| ---------- | ------------ | ------------- |
| 09-MAR-25  | 1            | 0             |
| 09-MAR-25  | 2            | 0             |
| 09-MAR-25  | 4            | 0 ❌           |
| …          | …            | …             |

***

### 🍂 02‑NOV‑2025

| EVENT\_DATE | HOUR\_ENDING | DST\_HR\_ID |
| ---------- | ------------ | ------------- |
| 02-NOV-25  | 1            | 0 ❌           |
| 02-NOV-25  | 2            | 0 ❌           |
| 02-NOV-25  | 2            | 1 ❌           |
| 02-NOV-25  | 3            | 0             |
| …          | …            | …             |

***

## ✅ To Be (after fix)

### 🌸 09‑MAR‑2025

| EVENT\_DATE | HOUR\_ENDING | DST\_HR\_ID |
| ---------- | ------------ | ------------- |
| 09-MAR-25  | 1            | 0             |
| 09-MAR-25  | 2            | 0             |
| 09-MAR-25  | 4            | 1 ✅           |
| …          | …            | …             |

***

### 🍂 02‑NOV‑2025

| EVENT\_DATE | HOUR\_ENDING | DST\_HR\_ID |
| ---------- | ------------ | ----------------------------- |
| 02-NOV-25  | 1            | 1 ✅                           |
| 02-NOV-25  | 2            | 1 ✅ (first occurrence – EDT)  |
| 02-NOV-25  | 2            | 0 ✅ (second occurrence – EST) |
| 02-NOV-25  | 3            | 0                             |
| …          | …            | …                             |

# 🏗 Constraint (important)

```sql
PRIMARY KEY (
  ZONE_ID, EVENT_DATE, HOUR_ENDING, DST_HR_ID
)
```

***

# 💡 Key Insight

| Row type                  | Can compute DST? | Action    |
| ------------------------- | ---------------- | --------- |
| Fall-back HE2 (duplicate) | ❌ ambiguous      | swap      |
| All other rows            | ✅ deterministic  | recompute |

***

# ✅ Solution

# 🧠 Script 1: Fix Duplicate Pairs

## 💡 Idea

> If a row has a “twin” with opposite DST → flip it

***

## ✅ SQL

```sql
-- Pseudocode:
-- FOR each row t IN table:
--     IF there exists some row d IN table
--        such that d matches t but has opposite dst_hr_id
--     THEN
--         flip t.dst_hr_id
UPDATE my_schema.my_table t -- When we do this we have one row in scope at a time. If I want to look for another row in the same table, I must reopen the table as a second copy like SELECT 1 FROM my_schema.my_table d. So t -> current row, d -> other rows to compare against
SET    t.dst_hr_id = 1 - t.dst_hr_id,
       t.last_updated_date = SYSTIMESTAMP,
       t.last_updated_by   = 'DstFixScript'
-- Update this row t IF there exists another row d19
-- with the same zone/date/hour but a different dst_hr_id
WHERE  EXISTS (
         SELECT 1 -- Just return something -- I don’t care what
         FROM   my_schema.my_table d
         WHERE  d.zone_id     = t.zone_id
         AND    d.event_date   = t.event_date
         AND    d.hour_ending = t.hour_ending
         AND    d.dst_hr_id <> t.dst_hr_id
       );
```

***

## 🧠 Example

```
Row A: HE2 dst=0
Row B: HE2 dst=1
Row C: HE4 dst=0
```

Step 1: Circle rows that need updating
  → Circle A
  → Circle B
Step 2: Flip the values of all circled rows at once

***

# 🧠 Script 2: Recompute ONLY Non-Duplicate Rows

## 💡 Idea

> If a row has NO twin → compute DST using timezone

***

## ✅ SQL

```sql
-- Pseudocode:
-- FOR each row t:
--     IF t DOES NOT have a twin:
--         calculate DST correctly using timezone
--     ELSE:
--         leave it alone (already fixed by script 1)
UPDATE my_schema.my_table t
SET    t.dst_hr_id =
         CASE
           WHEN EXTRACT(TIMEZONE_HOUR FROM -- Step3: extract offset. Offset -4  → EDT, -5  → EST
                FROM_TZ( -- Step2: attach timezone, so Oracle can determine DST vs standard
                  CAST(t.load_date + (t.hour_ending - 1)/24 AS TIMESTAMP),  -- Step1: DATE + hour → actual timestamp. For eg: 2025-03-09, HE=4 → 2025-03-09 03:00
                  'US/Eastern'
                )
           ) = -4 THEN 1
           ELSE 0
         END,
       t.last_updated_date = SYSTIMESTAMP,
       t.last_updated_by   = 'DstFixScript'
-- it's the inverse of script #1
WHERE  NOT EXISTS (
         SELECT 1
         FROM   my_schema.my_table d
         WHERE  d.zone_id     = t.zone_id
         AND    d.event_date   = t.event_date
         AND    d.hour_ending = t.hour_ending
         AND    d.dst_hr_id <> t.dst_hr_id
       );
```

***

## 🧠 Example

```
Row A: HE2 dst=0
Row B: HE2 dst=1
Row C: HE4 dst=0
```

**Step 1:** Circle rows that do NOT have a duplicate partner
  → Skip A (has B as twin)
  → Skip B (has A as twin)
  → Circle C (no twin)

**Step 2:** For each circled row, recompute DST using timezone:
  → If timestamp falls in EDT → set 1
  → If timestamp falls in EST → set 0

**Result:**
* A, B → unchanged (duplicate pair handled in Script 1)
* C    → recalculated correctly based on actual DST

***

# ⚠️ Important Notes

### ✅ Order matters

```
Run Script 1 FIRST
Run Script 2 SECOND
```

### ✅ Idempotency

* Script 1 → ❌ NOT idempotent
* Script 2 → ✅ idempotent
