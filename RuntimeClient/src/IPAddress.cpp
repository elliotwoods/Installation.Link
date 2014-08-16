
/*
 *  IPAddress.c
 *  PersonalProxy
 *
 *  Created by Chris Whiteford on 2009-02-20.
 *  Copyright 2009 __MyCompanyName__. All rights reserved.
 *
 */

#include "IPAddress.h"

#define	min(a,b)	((a) < (b) ? (a) : (b))
#define	max(a,b)	((a) > (b) ? (a) : (b))

#define BUFFERSIZE	4000

int nextAddr = 0;

vector<char*> IPAddress::if_names = vector<char*>(MAXADDRS);
vector<char*> IPAddress::ip_names = vector<char*>(MAXADDRS);
vector<unsigned long> IPAddress::ip_addrs = vector<unsigned long>(MAXADDRS);



void IPAddress::InitAddresses()
{
	int i;
	for (i=0; i<MAXADDRS; ++i)
	{
		if_names[i] = ip_names[i] = NULL;
		ip_addrs[i] = 0;
	}
}

void IPAddress::FreeAddresses()
{
	int i;
	for (i=0; i<MAXADDRS; ++i)
	{
		if (if_names[i] != 0) free(if_names[i]);
		if (ip_names[i] != 0) free(ip_names[i]);
		ip_addrs[i] = 0;
	}
	InitAddresses();
}

void IPAddress::GetIPAddresses()
{
	int                 i, len, flags;
	char                buffer[BUFFERSIZE], *ptr, lastname[IFNAMSIZ], *cptr;
	struct ifconf       ifc;
	struct ifreq        *ifr, ifrcopy;
	struct sockaddr_in	*sin;
	
	char temp[80];
	
	int sockfd;
	
	for (i=0; i<MAXADDRS; ++i)
	{
		if_names[i] = ip_names[i] = NULL;
		ip_addrs[i] = 0;
	}
	
	sockfd = socket(AF_INET, SOCK_DGRAM, 0);
	if (sockfd < 0)
	{
		perror("socket failed");
		return;
	}
	
	ifc.ifc_len = BUFFERSIZE;
	ifc.ifc_buf = buffer;
	
	if (ioctl(sockfd, SIOCGIFCONF, &ifc) < 0)
	{
		perror("ioctl error");
		return;
	}
	
	lastname[0] = 0;
	
	for (ptr = buffer; ptr < buffer + ifc.ifc_len; )
	{
		ifr = (struct ifreq *)ptr;
		len = max(sizeof(struct sockaddr), ifr->ifr_addr.sa_len);
		ptr += sizeof(ifr->ifr_name) + len;	// for next one in buffer
		
		if (ifr->ifr_addr.sa_family != AF_INET)
		{
			continue;	// ignore if not desired address family
		}
		
		if ((cptr = (char *)strchr(ifr->ifr_name, ':')) != NULL)
		{
			*cptr = 0;		// replace colon will null
		}
		
		if (strncmp(lastname, ifr->ifr_name, IFNAMSIZ) == 0)
		{
			continue;	/* already processed this interface */
		}
		
		memcpy(lastname, ifr->ifr_name, IFNAMSIZ);
		
		ifrcopy = *ifr;
		ioctl(sockfd, SIOCGIFFLAGS, &ifrcopy);
		flags = ifrcopy.ifr_flags;
		if ((flags & IFF_UP) == 0)
		{
			continue;	// ignore if interface not up
		}
		
		if_names[nextAddr] = (char *)malloc(strlen(ifr->ifr_name)+1);
		if (if_names[nextAddr] == NULL)
		{
			return;
		}
		strcpy(if_names[nextAddr], ifr->ifr_name);
		
		sin = (struct sockaddr_in *)&ifr->ifr_addr;
		strcpy(temp, inet_ntoa(sin->sin_addr));
		
		ip_names[nextAddr] = (char *)malloc(strlen(temp)+1);
		if (ip_names[nextAddr] == NULL)
		{
			return;
		}
		strcpy(ip_names[nextAddr], temp);
		
		ip_addrs[nextAddr] = sin->sin_addr.s_addr;
		
		++nextAddr;
	}
	
	close(sockfd);
}


