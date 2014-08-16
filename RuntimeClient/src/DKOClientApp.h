#pragma once
//copyright Elliot Woods 2010
//Kimchi and Chips
//http://www.kimchianchips.com

#include "ofMain.h"

#include "ofxiPhone.h"
#include "ofxiPhoneExtras.h"
#include "ofxOsc.h"

#include "ofxKCTouchGUI.h"
#include "ButtonGraphic.h"

#include "Countdown.h"
#include "ProgressBar.h"

#define D_SERVER_IP "192.168.1.10"
#define D_SEND_PORT 5556
#define RECEIVE_PORT 5555


#define NSCREENS 4
#define COUNTDOWN_SCREEN_INDEX 1
#define RECORD_SCREEN_INDEX 2
#define REVIEW_SCREEN_INDEX 3

#define MSG_INDICATE 0
#define MSG_RECORD_AGAIN 1
#define MSG_SAVE 3
#define MSG_CLEAR 4
#define MSG_RESELECT 5
#define MSG_STOP 6
#define MSG_SET_PRIORITY_1 7
#define MSG_SET_PRIORITY_0 8


class DKOClientApp : public ofxiPhoneApp {
	
public:
	
	~DKOClientApp();
	
	void setup();
	void longLoad();
	void update();
	void draw();
	void drawAddresses();
	
	void keyPressed(int key);
	void keyReleased(int key);
	
	void touchDown(ofTouchEventArgs &touch);
	void touchMoved(ofTouchEventArgs &touch);
	void touchUp(ofTouchEventArgs &touch);
	void touchDoubleTap(ofTouchEventArgs &touch);
	
	void lostFocus();
	void gotFocus();
	void gotMemoryWarning();
	void deviceOrientationChanged(int newOrientation);
	

private:
	void	advanceMode(const void* pSender, int &iAction);
	void	reselectScreen();
	void	reselectScreen(const void* pSender, int &iAction);
	void	stopRecording(const void* pSender, int &iAction);
	void	lastOptions(const void* pSender, int &iAction);
	
	void	reset();
	void	reset(const void* pSender, int &iAction);
	void	priority(const void* pSender, int &iAction);
	
	void	checkMessages();
	Touch			_Touches[MAX_TOUCHES];
	
	//debug
	stringstream		_strDebug;
	
	
	////////////////////////////////
	//Screens
	////////////////////////////////
	//
	vector<ofImage>	_vecScreenImages;
	//screen 0 - main
	ofImage			_imgStartPlaying;
	ofImage			_imgReselectPosition;
	ButtonGraphic	*_btnStartPlaying;
	ButtonGraphic	*_btnReselectPosition;
	//
	//screen 1 - countdown
	Countdown		_countdown;
	//
	//screen 2 - recording
	ProgressBar		_progress;
	ofImage			_imgProgress;
	ofImage			_imgStopRecording;
	ButtonGraphic	*_btnStopRecording;
	//
	//screen 3 - review
	ofImage			_imgSaveYourStory;
	ofImage			_imgRecordAgain;
	ButtonGraphic	*_btnSaveYourStory;
	ButtonGraphic	*_btnRecordAgain;
	//
	////////////////////////////////
	
	//Start again
	Button			*_btnReset;
	
	//Priority
	Button			*_btnPriority;
	bool			isPriority;
	void			setPriority(bool b);
	
	//need to select a new box
	bool			newBoxSelected;
	
	//mode
	int				_screen;
	float			_dampScreen;
	
	//classes
	ofxKCTouchGUI			_GUI;
	
	//OSC
	ofxOscSender	*_oscSender;
	ofxOscReceiver	_oscReceiver;
	string			_oscHost;
	int				_oscPortSend;
	
	//Sound
	ofSoundPlayer	sndCountdown;
	ofSoundPlayer	sndReview;
	

};