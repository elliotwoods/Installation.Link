/*
 *  Countdown.h
 *  DKO001 - iPad client
 *
 *  Created by Elliot Woods on 01/12/2010.
 *  Copyright 2010 Kimchi and Chips. All rights reserved.
 *
 */

#include "ofMain.h"

#define _COUNTDOWN_NIMAGES 80.0f
#define _COUNTDOWN_OFFSET 0.0f
#define _COUNTDOWN_LENGTH 4.0f

class Countdown
{
public:
	
	void		setup();
	void		draw();
	
	void		update();
	void		reset();
	
	vector<ofImage>		vecImages;
	
	float		time;
	
	bool		completed;
	
	float		timeStart;
};