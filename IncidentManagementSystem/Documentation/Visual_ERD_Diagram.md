# Visual ERD Diagram - Incident Management System

## Entity Relationship Diagram (Mermaid Format)

```mermaid
erDiagram
    Users {
        ObjectId _id PK
        string username UK "Unique, 3-50 chars"
        string email UK "Email format, unique"
        string passwordHash "Hashed password"
        string firstName "Required, max 50 chars"
        string lastName "Required, max 50 chars"
        enum role "Regular or ServiceDesk"
        string department "Optional reference"
        datetime createdAt "Auto-generated"
        datetime updatedAt "Auto-updated"
        boolean isActive "Default true"
        datetime lastLogin "Optional"
    }

    Tickets {
        ObjectId _id PK
        string title "Required, 5-200 chars"
        string description "Required, 10-2000 chars"
        enum status "Open, Resolved, Closed"
        enum priority "Low, Medium, High, Critical"
        enum category "Hardware, Software, Network, Access, Other"
        object reporter "Embedded UserInfo"
        object assignee "Optional embedded UserInfo"
        datetime createdAt "Auto-generated"
        datetime updatedAt "Auto-updated"
        datetime resolvedAt "Optional"
        datetime closedAt "Optional"
        array activities "Embedded TicketActivity[]"
        array comments "Embedded TicketComment[]"
    }

    Departments {
        ObjectId _id PK
        string name UK "Required, unique, max 100 chars"
        string description "Optional, max 500 chars"
        object manager "Optional embedded UserInfo"
        datetime createdAt "Auto-generated"
        boolean isActive "Default true"
    }

    UserInfo {
        ObjectId userId FK "Reference to Users._id"
        string username "Denormalized for performance"
        string email "Denormalized for performance"
        string fullName "Computed field"
        string department "Optional"
    }

    TicketActivity {
        string action "Created, Assigned, Updated, Resolved, Closed"
        object performedBy "Embedded UserInfo"
        datetime timestamp "When action occurred"
        string description "Optional details"
        string oldValue "Previous value for updates"
        string newValue "New value for updates"
    }

    TicketComment {
        ObjectId commentId PK "Auto-generated"
        object author "Embedded UserInfo"
        string content "Required, max 1000 chars"
        datetime timestamp "Auto-generated"
        boolean isInternal "Default false"
    }

    %% Relationships
    Users ||--o{ Tickets : "reporter.userId"
    Users ||--o{ Tickets : "assignee.userId"
    Users }o--|| Departments : "department name"
    Tickets ||--o{ TicketActivity : "embedded"
    Tickets ||--o{ TicketComment : "embedded"
    
    %% Embedded relationships
    Tickets ||--|| UserInfo : "reporter (embedded)"
    Tickets ||--o| UserInfo : "assignee (embedded)"
    TicketActivity ||--|| UserInfo : "performedBy (embedded)"
    TicketComment ||--|| UserInfo : "author (embedded)"
    Departments ||--o| UserInfo : "manager (embedded)"
```

## Collection Statistics (Meeting Requirements)

| Collection | Document Count | Status |
|-----------|---------------|---------|
| Users | 150 | ✅ Exceeds minimum 100 |
| Tickets | 200 | ✅ Exceeds minimum 100 |
| Departments | 10 | ✅ Complete organizational structure |
| **Total** | **360** | ✅ **Requirements exceeded** |

## Index Strategy Visualization

```mermaid
graph TD
    A[Users Collection] --> B[username - Unique Index]
    A --> C[email - Unique Index]
    A --> D[role - Single Field Index]
    A --> E[isActive - Single Field Index]
    
    F[Tickets Collection] --> G[reporter.userId - Single Field Index]
    F --> H[assignee.userId - Single Field Index]
    F --> I[status - Single Field Index]
    F --> J[priority - Single Field Index]
    F --> K[category - Single Field Index]
    F --> L[createdAt - Descending Index]
    F --> M[Compound: reporter.userId + status]
    F --> N[Compound: status + createdAt]
    F --> O[Text Index: title + description]
    
    P[Departments Collection] --> Q[name - Unique Index]
    P --> R[isActive - Single Field Index]
```

## Data Flow Architecture

```mermaid
flowchart TD
    A[Client Request] --> B[Controller Layer]
    B --> C[Repository Interface]
    C --> D[MongoDB Repository]
    D --> E[MongoDB Context]
    E --> F[MongoDB Database]
    
    F --> G[Users Collection<br/>150 documents]
    F --> H[Tickets Collection<br/>200 documents]
    F --> I[Departments Collection<br/>10 documents]
    
    J[Aggregation Pipeline] --> K[Dashboard Statistics]
    H --> J
    G --> J
    
    L[Seed Service] --> M[Generate Test Data]
    M --> F
```

## Design Decision Summary

### ✅ Embedding Strategy
- **User info in tickets**: Performance optimization for frequent queries
- **Activities in tickets**: Never accessed independently
- **Comments in tickets**: Contextual data access

### ✅ Indexing Strategy
- **Unique constraints**: Username, email uniqueness
- **Query optimization**: Compound indexes for dashboard queries
- **Search functionality**: Text indexes for content search

### ✅ Collection Design
- **Users**: Authentication and profile management
- **Tickets**: Core incident tracking with embedded data
- **Departments**: Organizational reference data

---

*This ERD represents the complete database design for the Incident Management System NoSQL project, meeting all requirements for Deliverable 1.*