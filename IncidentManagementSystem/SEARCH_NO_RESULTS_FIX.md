# Search Functionality Fix - No Results Issue

## Date: October 30, 2025

## Problem
The advanced search functionality was not returning any results when searching with search terms.

## Root Causes Identified

### 1. **Overly Restrictive User Filter**
**Problem:** Regular users could only see tickets where they were the reporter (`Reporter.UserId`), but not tickets assigned to them.

**Impact:** Users couldn't find tickets that were assigned to them, even if they matched the search criteria.

### 2. **Regex Pattern Issues**
**Problem:** Special characters in search terms weren't being escaped, which could cause regex errors or unexpected behavior.

**Impact:** Searches with special characters (like parentheses, brackets, etc.) would fail or not match correctly.

### 3. **Missing Logging**
**Problem:** No logging to help diagnose search issues.

**Impact:** Difficult to troubleshoot when searches returned no results.

---

## Solutions Implemented

### 1. **Expanded User Access Filter**
Changed the user filter to include BOTH reporter and assignee:

**Before:**
```csharp
if (!string.IsNullOrEmpty(userId))
{
    filters.Add(filterBuilder.Eq(t => t.Reporter.UserId, userId));
}
```

**After:**
```csharp
if (!string.IsNullOrEmpty(userId))
{
    // Search for tickets where user is either reporter OR assignee
    var userFilters = new List<FilterDefinition<Ticket>>
    {
        filterBuilder.Eq(t => t.Reporter.UserId, userId),
        filterBuilder.Eq("assignee.userId", userId)
    };
    filters.Add(filterBuilder.Or(userFilters));
}
```

**Benefit:** Regular users can now find tickets they created OR tickets assigned to them.

---

### 2. **Fixed Regex Escaping**
Added proper escaping for special characters in search terms:

**Before:**
```csharp
var textFilters = new List<FilterDefinition<Ticket>>
{
    filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(cleanTerm, "i")),
    filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(cleanTerm, "i"))
};
```

**After:**
```csharp
// Escape regex special characters to avoid regex errors
var escapedTerm = System.Text.RegularExpressions.Regex.Escape(cleanTerm);

var textFilters = new List<FilterDefinition<Ticket>>
{
    filterBuilder.Regex(t => t.Title, new MongoDB.Bson.BsonRegularExpression(escapedTerm, "i")),
    filterBuilder.Regex(t => t.Description, new MongoDB.Bson.BsonRegularExpression(escapedTerm, "i"))
};
```

**Benefit:** Searches with special characters now work correctly and safely.

---

### 3. **Added Logging for Diagnostics**
Added logging to track search operations:

```csharp
_logger.LogInformation("Search request - UserId: {UserId}, IsServiceDesk: {IsServiceDesk}, SearchText: '{SearchText}'", 
    currentUserId, isServiceDeskUser, model.SearchText);

// ... search execution ...

_logger.LogInformation("Search completed - Found {ResultCount} results", results.Count);
```

**Benefit:** Can now diagnose search issues by reviewing application logs.

---

## Files Modified

1. **Services/TicketSearchService.cs**
   - Expanded user filter to include assignee
   - Added regex escaping for search terms
   - Improved null reference handling

2. **Controllers/SearchController.cs**
   - Added ILogger dependency
   - Added diagnostic logging for search operations

---

## Testing the Fix

### Test Case 1: Search as Regular User
1. Log in as a regular user (non-service desk)
2. Create a ticket or have a ticket assigned to you
3. Go to Advanced Search
4. Enter a search term from the ticket title or description
5. **Expected:** Should find the ticket

### Test Case 2: Search with Special Characters
1. Create a ticket with title: "Network issue (urgent)"
2. Search for: "issue (urgent)"
3. **Expected:** Should find the ticket without regex errors

### Test Case 3: Search Assigned Tickets
1. Log in as a regular user
2. Have a service desk user assign a ticket to you
3. Search for terms in that ticket
4. **Expected:** Should find tickets assigned to you

### Test Case 4: AND/OR Operations
1. Create tickets with various keywords
2. Test "network AND server"
3. Test "email OR printer"
4. **Expected:** Both searches should return appropriate results

### Test Case 5: Check Logs
1. Perform a search
2. Check application logs
3. **Expected:** Should see log entries showing:
   - User ID performing search
   - Whether user is service desk
   - Search text used
   - Number of results found

---

## Common Issues and Solutions

### If Still No Results:

1. **Check if tickets exist in database**
   ```bash
   # Connect to MongoDB and check
   mongosh
   use IncidentManagementDB
   db.Tickets.countDocuments()
   ```

2. **Verify user ID matches tickets**
   - Check that Reporter.UserId or Assignee.UserId matches your user ID
   - Service desk users should see all tickets

3. **Check application logs**
   - Look for the search log entries
   - Verify the search criteria being used

4. **Test with empty search (no filters)**
   - Should return all tickets accessible to the user
   - If this returns nothing, the issue is likely data-related, not search logic

5. **Verify MongoDB connection**
   - Ensure MongoDB is running
   - Check connection string in appsettings.json
   - Verify database name is correct

---

## Summary of All Improvements

✅ **Fixed user access filter** - Users can now find tickets they reported OR were assigned  
✅ **Added regex escaping** - Special characters in search terms handled safely  
✅ **Improved logging** - Diagnostic information for troubleshooting  
✅ **Better error handling** - Clearer error messages and validation  
✅ **Fixed pagination** - JavaScript now works correctly with form fields  
✅ **Date filtering** - Proper start/end of day boundaries  
✅ **Service desk filters** - Assignee dropdown populated correctly  

The search functionality should now work correctly and return relevant results!
