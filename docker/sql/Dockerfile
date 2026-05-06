FROM mcr.microsoft.com/mssql/server:2022-latest

USER root

COPY 01-northwind.sql /docker-entrypoint-initdb.d/
COPY 02-shipping-geocodes.sql /docker-entrypoint-initdb.d/
COPY init-db.sh /docker-entrypoint-initdb.d/

# Strip Windows CRLF line endings and set executable permission
RUN sed -i 's/\r//' /docker-entrypoint-initdb.d/init-db.sh && \
    chmod +x /docker-entrypoint-initdb.d/init-db.sh

USER mssql

CMD /bin/bash -c '/opt/mssql/bin/sqlservr & /docker-entrypoint-initdb.d/init-db.sh && wait'