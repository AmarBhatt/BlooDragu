% Set up serial connection
port = 'Serial-COM5';
conn = serial(port);
get(conn, {'BaudRate','DataBits'})
% Open port
fopen(conn);
idn = fscanf(conn);
idn
fclose(conn);
