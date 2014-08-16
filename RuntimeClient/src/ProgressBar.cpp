/*
 *  ProgressBar.cpp
 *  DKO001 - iPad client
 *
 *  Created by Elliot Woods on 02/12/2010.
 *  Copyright 2010 Kimchi and Chips. All rights reserved.
 *
 */

#include "ProgressBar.h"

void ProgressBar::setup(int iScreen)
{
	x = 206 + ofGetWidth()*iScreen;
	y = 550;
	progress=0;
	
	imgGradient.loadImage("3progress.png");
}

void ProgressBar::draw()
{
	float xProgress = progress * _PROGRESS_MAX_WIDTH;
	
	imgGradient.draw(x, y, xProgress, 28);
	
	ofPushStyle();
	ofSetLineWidth(1);
	ofSetColor(0, 0, 0);
	ofLine(x+xProgress, y, x+xProgress, 550+28);
	ofPopStyle();

}