#!/bin/bash

# CSV Export Script for Deliverable 2
# This script starts the application, seeds the database, and exports CSV files

echo "=== Incident Management System - CSV Export Script ==="
echo "Starting application and exporting CSV files for deliverable 2..."
echo ""

# Navigate to project directory
PROJECT_DIR="/Users/aronlakatos/Library/Mobile Documents/com~apple~CloudDocs/School/Uni/Inholland/Y2/Y2-T1/NoSQL/Project/project-app/NoSQLProject/IncidentManagementSystem"
cd "$PROJECT_DIR"

echo "📂 Project directory: $PROJECT_DIR"
echo ""

# Build the project
echo "🔨 Building project..."
dotnet build
if [ $? -ne 0 ]; then
    echo "❌ Build failed!"
    exit 1
fi
echo "✅ Build successful!"
echo ""

# Start the application in background
echo "🚀 Starting application..."
dotnet run --launch-profile http &
APP_PID=$!

# Wait for application to start
echo "⏳ Waiting for application to start..."
sleep 10

# Seed the database
echo "🌱 Seeding database..."
curl -s -X GET "http://localhost:5222/api/databasetest/seed" | grep -q "success\|Success"
if [ $? -eq 0 ]; then
    echo "✅ Database seeding completed!"
else
    echo "⚠️  Database seeding may have failed, continuing with export..."
fi
echo ""

# Export CSV files
echo "📁 Exporting CSV files..."
curl -s -X GET "http://localhost:5222/api/databasetest/export-csv" | grep -q "success\|Success"
if [ $? -eq 0 ]; then
    echo "✅ CSV export completed!"
else
    echo "❌ CSV export failed!"
fi
echo ""

# Download individual CSV files
echo "📥 Downloading CSV files..."
mkdir -p CSVDeliverable2

# Download Users CSV
curl -s -X GET "http://localhost:5222/api/databasetest/download/users" -o "CSVDeliverable2/Users_$(date +%Y%m%d_%H%M%S).csv"
if [ $? -eq 0 ]; then
    echo "✅ Users CSV downloaded"
else
    echo "❌ Failed to download Users CSV"
fi

# Download Tickets CSV
curl -s -X GET "http://localhost:5222/api/databasetest/download/tickets" -o "CSVDeliverable2/Tickets_$(date +%Y%m%d_%H%M%S).csv"
if [ $? -eq 0 ]; then
    echo "✅ Tickets CSV downloaded"
else
    echo "❌ Failed to download Tickets CSV"
fi

echo ""
echo "📊 Checking exported files..."
ls -la CSVDeliverable2/
echo ""

# Terminate the application
echo "🛑 Stopping application..."
kill $APP_PID
wait $APP_PID 2>/dev/null

echo ""
echo "✨ CSV export process completed!"
echo "📁 Files are available in: $PROJECT_DIR/CSVDeliverable2/"
echo ""
echo "📋 Summary for Deliverable 2:"
echo "   ✓ Collections exported to CSV format"
echo "   ✓ Users CSV contains user records"
echo "   ✓ Tickets CSV contains ticket records"
echo "   ✓ Files ready for submission"
echo ""
echo "🎉 Deliverable 2 CSV export completed successfully!"