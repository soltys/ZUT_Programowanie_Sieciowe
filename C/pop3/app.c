#include <stdio.h>
#include <stdlib.h>
#include <errno.h>
#include <netdb.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <string.h>
#include <fcntl.h> 
#include <unistd.h> 
#include <string.h>

void err(char *where) {
    fprintf(stderr, "error in %s: %d\n", where, errno);
    exit(1);
}



int main(int argc, char *argv[]) {
    if (argc != 4){
        printf("Usage: chkmail [user name] [host address] [password]\n");
        exit(1);
    }

    char* userName = argv[1];   

    char *remote = argv[2];
    char* password = argv[3];

    struct servent *sent;
    struct protoent *pent;
    int  port;
    int  sock;
    int  result;
    in_addr_t ipadr;
    struct sockaddr_in addr;
    struct hostent *hent;
    char buf[2048];

    sent = getservbyname("pop3", "tcp");
    if(sent == NULL)
        err("getservbyname");
    port = sent->s_port;
    pent = getprotobyname("tcp");
    if(pent == NULL)
        err("getprotobyname");
    hent = gethostbyname(remote);
    if (hent == NULL){
        err("remote do not exists");
    }
    //printf("Host: %s\n", hent->h_name);
    //printf("IP: %s\n", inet_ntoa(*((struct in_addr *)hent->h_addr)));
    addr.sin_family = AF_INET;
    addr.sin_port = port;
    addr.sin_addr = *((struct in_addr *)hent->h_addr);
    memset(addr.sin_zero, '\0', 8);
    sock = socket(AF_INET, SOCK_STREAM, pent->p_proto);
    if(sock < 0)
        err("socket");
    result = connect(sock, (struct sockaddr *)&addr, sizeof(struct sockaddr));
    if(result < 0)
        err("connect");

    result = read(sock, buf, sizeof(buf));
    buf[result] = '\0'; 
    //printf(buf);
   // printf("----\n");

    strcpy(buf,"");

    char *msg = buf;    
    msg = stpcpy(msg, "USER ");    
    msg = stpcpy(msg, userName);    
    msg = stpcpy(msg, "\r\n\0");    
    //printf(buf);
    
    write(sock, buf, strlen(buf));
    result = read(sock, buf, sizeof(buf));
    buf[result] = '\0'; 
    //printf(buf);
    
    strcpy(buf,"");
    msg = buf;    
    msg = stpcpy(msg, "PASS ");    
    msg = stpcpy(msg, password);    
    msg = stpcpy(msg, "\r\n\0");    
    //printf(buf);

    write(sock, buf, strlen(buf));
    result = read(sock, buf, sizeof(buf));
        buf[result] = '\0'; 
        //printf(buf);
    

    //printf("LIST\n");    
    strcpy(buf, "LIST\r\n");
    write(sock, buf, strlen(buf));
    result = read(sock, buf, sizeof(buf));
    buf[result] = '\0'; 
    //printf(buf);
    
    result = read(sock, buf, sizeof(buf));
    buf[result] = '\0'; 
    //printf(buf);

    char *token;
    token = strtok(buf,"\n");
    int mailCount = 0;
    if (token != NULL)
    {
        
        mailCount++;
        while((token = strtok(NULL,"\n")) != NULL){  
            mailCount++;
        }
    }    

    printf("%d \n", mailCount-1);


    //printf("QUIT");    
    strcpy(buf, "QUIT\r\n");
    write(sock, buf, strlen(buf));
    result = read(sock, buf, sizeof(buf));
    buf[result] = '\0'; 
    //printf(buf);
    

    close(sock);
    return 0;
}
