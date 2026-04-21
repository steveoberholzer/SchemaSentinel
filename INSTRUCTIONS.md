# SchemaSentinel

## Purpose

SchemaSentinel is a proposed **C# WPF desktop tool** for comparing SQL Server database schemas in a way that is more useful than traditional compare tools when the goal is to detect **meaningful drift** rather than every literal difference.

The primary use case is comparing environments such as **DEV vs STAGE**, while reducing noise from differences that are technically present but not important to the comparison, such as **column order**.

The tool should support both:

- **SQL Server on-premises**
- **Azure SQL Database / SQL Azure**

The intention is to allow a user to:

- compare two databases
- focus on only selected object types
- ignore non-semantic differences where appropriate
- filter to objects changed since a chosen date
- generate a readable report of the differences

---

## Why build this?

Generic schema comparison tools are useful, but they often surface too much noise for day-to-day support or investigative work. In particular:

- column order differences can distract from real structural differences
- formatting differences in SQL modules can produce false positives
- it is often desirable to compare only **recently changed** objects
- developers and support staff may want **logical comparison** rather than exact textual comparison

SchemaSentinel would aim to bridge the gap between a DBA-grade comparison engine and a practical investigation tool for developers and support consultants.

---

## Suggested tool names

### Recommended
**SchemaSentinel**

Reason:
- sounds purposeful and professional
- implies watching for schema drift
- broad enough to cover tables, views, procedures, and functions

### Other possible names
- **DriftScope**
- **SchemaLens**
- **DBDiff Inspector**
- **SchemaHarbour**
- **StructureWatch**

If a more technical or enterprise-style name is preferred, **SchemaSentinel** or **DriftScope** are probably the strongest options.

---

## High-level goals

The tool should:

1. Connect to two SQL databases
2. Extract comparable metadata from both
3. Normalize the object definitions before comparing them
4. Ignore selected noise such as column order
5. Filter by object type, schema, and optional changed-since date
6. Present a clear difference report in the UI
7. Support exporting results for later review

---

## Target platform

- **Language:** C#
- **UI:** WPF
- **Data access:** Microsoft.Data.SqlClient
- **Target environments:**
  - SQL Server (on-prem)
  - Azure SQL Database

---

## Core features

### 1. Dual database connection support
The tool should allow the user to define:

- Source database connection
- Target database connection

Each connection should support:

- Server name / fully qualified Azure SQL server name
- Database name
- Authentication type
  - Windows Authentication (on-prem where applicable)
  - SQL Authentication
  - Azure SQL compatible authentication options as required later
- Optional connection timeout
- Optional ability to save named connection profiles

### 2. Compare selected object types
Initial support should include:

- Tables
- Views
- Stored Procedures
- Functions

Potential future support:

- Triggers
- Synonyms
- Indexes
- Foreign Keys
- Defaults
- Check Constraints
- User-defined Types

### 3. Ignore column order differences
For table comparison, the tool should be able to compare columns in a **logical order** instead of physical ordinal position.

Example option:
- **Ignore column order**

When enabled, the comparison engine should sort columns into a stable order before comparing, such as:

- alphabetical by column name
- or another consistent canonical ordering

This should reduce noise when two tables are structurally identical apart from column placement.

### 4. Optional alphabetical column sorting in output
The user should be able to choose whether the rendered table definitions in the comparison output are shown:

- in physical database order
- or sorted alphabetically by column name

This is especially useful when visually inspecting large tables.

### 5. Changed-since filtering
The tool should allow comparison of only objects changed since a selected date.

This feature is important when:

- work was done in DEV during a known time window
- there is suspicion that someone made changes in STAGE after a certain point
- a smaller, time-bound comparison is needed instead of a full comparison

#### Important note
For programmable objects such as:
- Views
- Stored Procedures
- Functions

`sys.objects.modify_date` is usually helpful.

For tables, `modify_date` can still be useful, but may not be a perfect audit source for every structural change. Because of that, the tool should present this as a **best-effort filter**, unless a later history/snapshot feature is added.

### 6. Comparison modes
The tool should ideally support two modes:

#### Strict mode
Compare the objects as literally as practical.

Use when:
- exact drift matters
- deployment parity is being audited
- naming and ordering differences are relevant

#### Logical mode
Normalize definitions before comparing and ignore selected non-semantic noise.

Use when:
- the goal is to find meaningful differences
- column order should be ignored
- whitespace/casing differences should be ignored

### 7. Human-readable results
The results should not just say that an object is different. They should explain **how** it is different.

Examples:
- Missing in Source
- Missing in Target
- Changed
- Identical

For changed objects, the tool should show:
- summary of what changed
- normalized view of the object
- optionally a side-by-side textual diff

### 8. Exportable reports
Useful export formats could include:

- Markdown
- HTML
- JSON
- plain text

HTML is likely the most useful polished report format for sharing.

---

## Recommended architecture

A clean separation of responsibilities would help a lot.

### Suggested project layout

- **SchemaSentinel.UI**  
  WPF application

- **SchemaSentinel.Core**  
  comparison models, normalization logic, diff logic

- **SchemaSentinel.Data**  
  SQL metadata extraction and connection handling

- **SchemaSentinel.Reporting**  
  export to HTML / Markdown / JSON

If desired, this could start as a smaller solution and be split later.

---

## Conceptual workflow

1. User opens the application
2. User selects source and target connections
3. User chooses:
   - object types
   - schemas
   - changed-since date (optional)
   - comparison mode
   - ignore column order option
4. Application extracts metadata from both databases
5. Application normalizes each object into a canonical representation
6. Application compares canonical representations
7. UI shows summary and details
8. User exports report if needed

---

## Metadata extraction approach

The tool should avoid relying on generated scripts from external compare tools. Instead, it should query SQL Server metadata directly.

### Useful SQL Server catalog views
Depending on object type, the following will likely be relevant:

- `sys.objects`
- `sys.schemas`
- `sys.tables`
- `sys.columns`
- `sys.types`
- `sys.default_constraints`
- `sys.check_constraints`
- `sys.indexes`
- `sys.index_columns`
- `sys.key_constraints`
- `sys.foreign_keys`
- `sys.foreign_key_columns`
- `sys.views`
- `sys.procedures`
- `sys.sql_modules`
- `sys.parameters`

This metadata can then be transformed into canonical models.

---

## Canonical comparison model

The key to making this tool useful is to compare a **normalized representation** of each object, not the raw database script alone.

### Table canonical model
A table model may include:

- Schema name
- Table name
- Columns
  - name
  - data type
  - max length
  - precision
  - scale
  - nullability
  - identity properties
  - computed definition
  - default definition
- Primary key
- Unique constraints
- Foreign keys
- Indexes

When **Ignore column order** is enabled, the column collection should be sorted into a stable comparison order before hashing or diffing.

### View / Procedure / Function canonical model
For programmable objects, a canonical model may include:

- Schema name
- Object name
- Object type
- Normalized definition text
- Parameters (where relevant)
- Modify date

The normalized definition text should remove obvious formatting noise while preserving actual logic changes.

---

## Normalization ideas

### Tables
Potential normalization rules:

- optionally sort columns alphabetically
- standardize data type names where necessary
- normalize default constraint expressions
- optionally ignore constraint names
- optionally ignore index names
- standardize whitespace in computed expressions

### Views / Stored Procedures / Functions
Potential normalization rules:

- normalize line endings
- trim trailing spaces
- collapse excessive blank lines
- optionally normalize casing
- remove `SET ANSI_NULLS` / `SET QUOTED_IDENTIFIER` noise if desired
- standardize bracket usage if practical

The first version should avoid trying to fully parse T-SQL semantics. Text normalization is enough to make a solid first release.

---

## Handling "changed since X date"

This is a valuable feature, but it should be implemented with realistic expectations.

### Practical first version
Use metadata such as:

- `sys.objects.modify_date`

This is generally helpful for:
- views
- procedures
- functions
- some table changes

### Important caveat
For tables and some child components, `modify_date` is not a perfect historical audit trail for every possible schema change. Therefore, the UI should make it clear that this is a practical filter rather than a guaranteed full audit mechanism.

### Better future version
Add a snapshot/history feature:

- extract normalized objects on each run
- generate a hash per object
- persist snapshots locally
- compare current state with prior snapshots

That would allow accurate tracking of:
- what changed since last run
- what changed since a chosen date
- which environment drifted over time

---

## WPF UI ideas

### Main window layout

#### Left/top: connection setup
- Source connection panel
- Target connection panel
- Test connection buttons

#### Filter panel
- Object type checkboxes
- Schema selector
- Changed since date picker
- Comparison mode selector
- Ignore column order checkbox
- Sort columns alphabetically checkbox
- Ignore whitespace checkbox
- Ignore casing checkbox

#### Main results area
- Summary counts
  - Missing in Source
  - Missing in Target
  - Changed
  - Identical
- Object list grid
- Difference detail viewer
- Side-by-side text panel for definitions

#### Footer/toolbar actions
- Compare
- Export
- Save profile
- Reload

---

## Suggested result model

### Comparison summary
- total objects scanned
- identical objects
- changed objects
- objects missing in source
- objects missing in target

### Per-object result
- object type
- schema
- object name
- source exists
- target exists
- status
- summary message
- detailed differences
- source normalized definition
- target normalized definition

---

## Suggested development phases

## Phase 1 - Minimal useful version
Build a working version that supports:

- connecting to two databases
- comparing tables, views, procedures, and functions
- ignore column order for tables
- sort columns alphabetically in comparison output
- changed-since filter using metadata
- strict vs logical mode
- WPF results grid

This phase would already be very useful.

## Phase 2 - Better table intelligence
Add:

- primary keys
- foreign keys
- indexes
- default constraints
- check constraints
- richer table diff summaries

## Phase 3 - Better programmable object comparison
Add:

- improved SQL normalization
- side-by-side diff viewer
- ignore selected SQL boilerplate
- parameter comparison for procedures/functions

## Phase 4 - Snapshot history
Add:

- local history store
- object hashing
- compare against prior runs
- reliable changed-since reporting based on snapshots

## Phase 5 - Reporting and usability polish
Add:

- export to HTML / Markdown / JSON
- saved comparison profiles
- object search
- grouping by schema or type
- dark mode / polished presentation

---

## Technical considerations

### Connectivity
Use `Microsoft.Data.SqlClient` and design the connection layer to handle:

- on-prem SQL Server
- Azure SQL Database
- encrypted connections
- trust server certificate option where required for internal environments
- connection test and error display

### Security
Do not store passwords in plain text.

If connection profiles are persisted, consider:
- Windows Credential Manager
- DPAPI encryption
- prompting for secrets at runtime

### Performance
For large databases:

- load metadata asynchronously
- compare objects off the UI thread
- allow cancellation
- consider caching extracted metadata during a session

### Reliability
The tool should tolerate:
- permission gaps
- partial object extraction failures
- network issues
- unsupported object edge cases

Comparison should continue where possible and report failures clearly.

---

## Risks and limitations

### 1. T-SQL semantic equivalence is hard
Two SQL module definitions may be logically equivalent but textually different. Full semantic SQL comparison is a major undertaking and should not be the goal of the first version.

### 2. Modify date is not a perfect audit source
The changed-since feature will be helpful, but not absolute, unless backed by snapshots.

### 3. Object naming can create noise
Constraint names and index names may differ even when their functional purpose is the same. The tool may need optional settings to ignore or soften name-only differences.

---

## Recommended first implementation stance

Keep the first version practical and focused.

### Start with:
- C# WPF desktop app
- SQL Server + Azure SQL support
- Tables + Views + Stored Procedures + Functions
- strict and logical comparison modes
- ignore column order option
- changed-since filtering
- readable results grid

### Avoid in version 1:
- full SQL parser
- deployment script generation
- trying to perfectly model every SQL Server edge case

The goal of version 1 should be trustworthiness, readability, and usefulness.

---

## Example success criteria

The first useful version of SchemaSentinel can be considered successful if it allows a user to:

- compare DEV and STAGE quickly
- ignore column order differences in tables
- filter to only objects changed after a given date
- clearly see changes in views, procedures, and functions
- export a readable summary of drift between environments

---

## Final recommendation

This is a worthwhile tool to build.

A **C# WPF** application is a strong fit for the requirement, especially if the goal is a practical desktop utility for internal use. The feature set is achievable in stages, and even a modest first release would likely be valuable immediately.

The best starting point is:

1. build a solid metadata extraction layer
2. define canonical object models
3. implement logical normalization rules
4. present the comparison cleanly in WPF

If the project grows later, a snapshot/history layer can turn it from a compare tool into a proper schema drift investigation tool.

---

## Suggested next step for future implementation

When work begins, the next design artifact to produce should be:

- a concrete solution structure
- class models for each object type
- initial SQL catalog queries
- normalization rules per object type
- a WPF screen mock-up / MVVM layout

