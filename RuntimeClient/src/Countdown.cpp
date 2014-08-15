/*
 *  Countdown.cpp
 *  DKO001 - iPad client
 *
 *  Created by Elliot Woods on 01/12/2010.
 *  Copyright 2010 Kimchi and Chips. All rights reserved.
 *
 */

#include "Countdown.h"

void Countdown::setup()
{
	string strSeq, strFilename;
	
	for (int i=1; i<_COUNTDOWN_NIMAGES+1; i++) {
		vecImages.push_back(ofImage());
		
		strSeq = ofToString(i, 0);
		
		//pad with 0's
		while (strSeq.length()<4)
			strSeq = "0" + strSeq;
		
		strFilename = "recording_sequence/recording1"+strSeq+".png";
		ofLog(OF_LOG_VERBOSE, strFilename);
		vecImages.back().loadImage(strFilename);
	}
	
	time=0;
	timeStart=0;
	completed=false;
}

void Countdown::draw()
{	
	if (completed)
		vecImages.back().draw(295,412);
	else
		vecImages[ofClamp(time / _COUNTDOWN_LENGTH * (_COUNTDOWN_NIMAGES - _COUNTDOWN_OFFSET) + _COUNTDOWN_OFFSET, 0, _COUNTDOWN_NIMAGES-1)].draw(295,412);
}

void Countdown::update()
{
	time = ofGetElapsedTimef() - timeStart;
	
	if (time>_COUNTDOWN_LENGTH)
	{
		time = _COUNTDOWN_LENGTH;
		completed=true;
	}
}

void Countdown::reset()
{
	time = 0;
	timeStart = ofGetElapsedTimef();
	completed = false;
}