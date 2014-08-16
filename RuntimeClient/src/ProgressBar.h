/*
 *  ProgressBar.h
 *  DKO001 - iPad client
 *
 *  Created by Elliot Woods on 02/12/2010.
 *  Copyright 2010 Kimchi and Chips. All rights reserved.
 *
 */

#include "ofMain.h"

#define _PROGRESS_MAX_WIDTH 359.0f
class ProgressBar
{
public:
	void		setup(int iScreen);
	void		draw();
	
	float		progress;
	ofImage		imgGradient;
	
	float		x, y;
};