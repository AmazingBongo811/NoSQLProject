# Quick Troubleshooting Guide for Search

## If Search Still Returns No Results

### Step 1: Verify MongoDB is Running
```bash
brew services list | grep mongodb
# Should show "started"
```

### Step 2: Check if Tickets Exist in Database
```bash
# Connect to MongoDB
mongosh

# Switch to your database
use IncidentManagementDB

# Count total tickets
db.Tickets.countDocuments()

# View sample tickets
db.Tickets.find().limit(3).pretty()

# Check a specific ticket structure
db.Tickets.findOne()
```

### Step 3: Verify User Can Access Tickets
```bash
# Still in mongosh
# Replace YOUR_USER_ID with your actual user ID

# Find tickets you reported
db.Tickets.find({ "reporter.userId": "YOUR_USER_ID" }).count()

# Find tickets assigned to you
db.Tickets.find({ "assignee.userId": "YOUR_USER_ID" }).count()

# Sample ticket with your user
db.Tickets.findOne({ 
  $or: [
    { "reporter.userId": "YOUR_USER_ID" },
    { "assignee.userId": "YOUR_USER_ID" }
  ]
})
```

### Step 4: Test Simple Search Query Manually
```bash
# Still in mongosh
# This mimics what the search does

# Case-insensitive search for "network" in title
db.Tickets.find({ 
  title: { $regex: "network", $options: "i" }
}).count()

# Search in title OR description
db.Tickets.find({ 
  $or: [
    { title: { $regex: "network", $options: "i" } },
    { description: { $regex: "network", $options: "i" } }
  ]
}).count()
```

### Step 5: Check Your User ID
In the application:
1. Log in
2. Open browser developer tools (F12)
3. Go to Console tab
4. Type: `document.cookie`
5. Look for your user ID in the authentication cookie

Or check in the database:
```bash
# In mongosh
db.Users.find({}, { _id: 1, firstName: 1, lastName: 1, email: 1, role: 1 }).pretty()
```

### Step 6: Test Search Without Filters
1. Go to Advanced Search page
2. Leave ALL fields empty
3. Click Search button
4. **Should return:** All tickets you have access to (up to page size limit)

If this returns nothing, the issue is with user access, not search terms.

### Step 7: Test as Service Desk User
Service desk users should see ALL tickets regardless of reporter/assignee.

1. Log in as a service desk user
2. Go to Advanced Search
3. Leave filters empty
4. Click Search
5. **Should return:** ALL tickets in the system

### Step 8: Check Application Logs
When you perform a search, check the console output:
```
info: IncidentManagementSystem.Controllers.SearchController[0]
      Search request - UserId: 507f1f77bcf86cd799439011, IsServiceDesk: False, SearchText: 'network'
info: IncidentManagementSystem.Controllers.SearchController[0]
      Search completed - Found 5 results
```

If you see "Found 0 results" but expected more, the issue is likely:
- User access (not reporter or assignee)
- Search text doesn't match any tickets
- No tickets exist in database

---

## Quick Data Seed Test

If your database is empty, seed some test data:

```bash
cd "/path/to/IncidentManagementSystem"
dotnet run seeddata
```

Or use the application's seed functionality (if implemented).

---

## Test Tickets to Create Manually

Create these tickets for testing:

1. **Ticket 1:**
   - Title: "Network connection issue"
   - Description: "Unable to connect to server"
   - Status: Open
   - Priority: High

2. **Ticket 2:**
   - Title: "Email not working"
   - Description: "Cannot send emails from Outlook"
   - Status: Open
   - Priority: Medium

3. **Ticket 3:**
   - Title: "Printer offline"
   - Description: "Network printer not responding"
   - Status: Resolved
   - Priority: Low

Then test searches:
- "network" → Should find Tickets 1 & 3
- "email OR printer" → Should find Tickets 2 & 3
- "network AND server" → Should find Ticket 1
- "outlook" → Should find Ticket 2

---

## MongoDB Connection String Check

Verify in `appsettings.json`:
```json
{
  "MongoDB": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "IncidentManagementDB"
  }
}
```

Test connection:
```bash
mongosh "mongodb://localhost:27017/IncidentManagementDB"
```

If connection fails:
1. Start MongoDB: `brew services start mongodb-community`
2. Check MongoDB logs: `tail -f /usr/local/var/log/mongodb/mongo.log`
3. Verify port 27017 is open: `lsof -i :27017`

---

## Common Solutions

### Problem: "Found 0 results" but tickets exist
**Solution:** User is not the reporter or assignee. Either:
- Log in as the user who created the tickets
- Assign tickets to your user
- Log in as a service desk user

### Problem: Search with special characters fails
**Solution:** Already fixed with regex escaping. Update code if using old version.

### Problem: Pagination doesn't work
**Solution:** Already fixed with querySelector. Clear browser cache.

### Problem: Date filters don't work
**Solution:** Already fixed with proper date boundaries. Ensure dates are set correctly.

---

## Need More Help?

1. Check the two documentation files:
   - `SEARCH_IMPROVEMENTS.md` - Initial fixes
   - `SEARCH_NO_RESULTS_FIX.md` - No results issue fix

2. Review application logs in terminal

3. Test MongoDB connection and data directly

4. Verify user roles and permissions
