#pragma once
/*
 *  IPAddress.h
 *  PersonalProxy
 *
 *  Created by Chris Whiteford on 2009-02-20.
 *  Copyright 2009 __MyCompanyName__. All rights reserved.
 *
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netdb.h>
#include <arpa/inet.h>
#include <sys/sockio.h>
#include <net/if.h>
#include <errno.h>
#include <net/if_dl.h>
#include <vector>

using namespace std;

#define MAXADDRS 32
// Function prototypes
class IPAddress {
public:
	static void InitAddresses();
	static void FreeAddresses();
	static void GetIPAddresses();	
	
	static vector<char* > if_names;
	static vector<char* > ip_names;
	static vector<unsigned long> ip_addrs;
};

