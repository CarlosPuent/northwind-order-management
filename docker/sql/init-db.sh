#!/bin/bash
echo "Waiting for SQL Server to be ready..."

# Wait until SQL Server accepts connections
for i in {1..60}; do
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT 1" > /dev/null 2>&1
    if [ $? -eq 0 ]; then
        echo "SQL Server is ready."
        break
    fi
    echo "Waiting... ($i/60)"
    sleep 2
done

# Check if Northwind already exists
DB_EXISTS=$(/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM sys.databases WHERE name='Northwind'" -h -1 | tr -d ' ')

if [ "$DB_EXISTS" = "0" ]; then
    echo "Installing Northwind database..."
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -i /docker-entrypoint-initdb.d/01-northwind.sql
    echo "Northwind installed."
else
    echo "Northwind database already exists. Skipping install."
fi

echo "Applying ShippingGeocodes migration..."
/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -i /docker-entrypoint-initdb.d/02-shipping-geocodes.sql
echo "Database initialization complete."