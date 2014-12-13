#pragma once

#include "ofMain.h"

#include "ofxiOS.h"
#include "ofxiOSExtras.h"
#include "ofxKCTouchGui.h"
#include "ofxOsc.h"

#include "StartButton.h"
#include "Countdown.h"
#include "Recording.h"

#define RES_MULT 1

class ofApp : public ofxiOSApp{
    
public:
	enum State {
		State_Waiting,
		State_CountingDown,
		State_Recording
	};
	
	void setup();
	void update();
	void draw();

	void exit();

	void touchDown(ofTouchEventArgs & touch);
	void touchMoved(ofTouchEventArgs & touch);
	void touchUp(ofTouchEventArgs & touch);
	void touchDoubleTap(ofTouchEventArgs & touch);
	void touchCancelled(ofTouchEventArgs & touch);

	void lostFocus() { }
	void gotFocus() { }
	void gotMemoryWarning() { }
	void deviceOrientationChanged(int newOrientation) { }
	
	void gotoCountdown();
	void gotoRecording();
	void gotoWaiting();
	
	ofxKCTouchGui::Controller gui;
	shared_ptr<StartButton> startButton;
	shared_ptr<Countdown> countdown;
	shared_ptr<Recording> recording;
	
	ofFbo fbo;
	
	State state;
	
	ofxOscSender osc;
};

