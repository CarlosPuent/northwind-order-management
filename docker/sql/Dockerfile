FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

# Copy SQL scripts and init script
COPY 01-northwind.sql /docker-entrypoint-initdb.d/
COPY 02-shipping-geocodes.sql /docker-entrypoint-initdb.d/
COPY init-db.sh /docker-entrypoint-initdb.d/

RUN chmod +x /docker-entrypoint-initdb.d/init-db.sh

USER mssql

# Start SQL Server and run init script in background
CMD /bin/bash -c '/opt/mssql/bin/sqlservr & /docker-entrypoint-initdb.d/init-db.sh && wait'