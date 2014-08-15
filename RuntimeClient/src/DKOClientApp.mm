#include "DKOClientApp.h"
#include "IPAddress.h"
//copyright Elliot Woods 2010
//Kimchi and Chips
//http://www.kimchiandchips.com

//--------------------------------------------------------------

DKOClientApp::~DKOClientApp()
{
	isPriority = false;
	
}

void DKOClientApp::setup()
{
	ofBackground(0, 0, 0);
}

void DKOClientApp::longLoad()
{
	
	// register touch events
	//ofRegisterTouchEvents(this);
	//
	//iPhoneAlerts will be sent to this.
	//ofxiPhoneAlerts.addListener(this);
	//
	//now redundant
	
	//setup some blank touches
	for (int iTouch=0; iTouch<MAX_TOUCHES; iTouch++)
		_Touches[iTouch].ID = iTouch;
	
	ofSetLogLevel(OF_LOG_VERBOSE);
	
	////////////////////////////////////////////////////////
	// SETUP GRAPHICS
	////////////////////////////////////////////////////////
	//
	ofSetFrameRate(30);
	ofSetVerticalSync(true);
	ofEnableSmoothing(); //pretty sure this doesn't work
	ofxiPhoneSetOrientation(OFXIPHONE_ORIENTATION_PORTRAIT);
	//
	////////////////////////////////////////////////////////
	
	
	////////////////////////////////////////////////////////
	// SETUP OSC
	////////////////////////////////////////////////////////
	//
	_oscSender = new ofxOscSender();
	_oscSender->setup(D_SERVER_IP, D_SEND_PORT);
	_oscReceiver.setup(RECEIVE_PORT);
	_oscHost = D_SERVER_IP;
	_oscPortSend = D_SEND_PORT;
	//
	////////////////////////////////////////////////////////
	
	
	////////////////////////////////////////////////////////
	// SETUP BUTTONS
	////////////////////////////////////////////////////////
	//
	_imgStartPlaying.loadImage("btn_record.png");
	_imgReselectPosition.loadImage("btn_changebox.png");
	_imgStopRecording.loadImage("btn_stop.png");
	_imgSaveYourStory.loadImage("btn_save.png");
	_imgRecordAgain.loadImage("btn_recordagain.png");
	//
	_btnReselectPosition = new ButtonGraphic(0, &_imgReselectPosition,
											 112, 777, 0,
											 1);
	_btnStartPlaying = new ButtonGraphic(0, &_imgStartPlaying,
										 353,777, 0,
										 0);

	_btnStopRecording = new ButtonGraphic(0, &_imgStopRecording,
										  272, 777, 0,
										  0);
	
	_btnSaveYourStory = new ButtonGraphic(0, &_imgSaveYourStory,
										  112, 777, 0,
										  0);
	
	_btnRecordAgain = new ButtonGraphic(0, &_imgRecordAgain,
										353, 777, 0,
										1);
	//
	_btnStartPlaying->action+= Delegate<DKOClientApp, int>(this, &DKOClientApp::advanceMode);
	
	_btnReselectPosition->action+= Delegate<DKOClientApp, int>(this, &DKOClientApp::reselectScreen);
	
	_btnStopRecording->action+= Poco::Delegate<DKOClientApp, int>(this, &DKOClientApp::stopRecording);
	
	_btnSaveYourStory->action+= Delegate<DKOClientApp, int>(this, &DKOClientApp::lastOptions);
	
	_btnRecordAgain->action+= Delegate<DKOClientApp, int>(this, &DKOClientApp::lastOptions);
	//
	////////////////////////////////////////////////////////
	
	
	////////////////////////////////////////////////////////
	// SETUP RESET BUTTON
	////////////////////////////////////////////////////////
	//
	_btnReset = new Button(ofGetWidth()-100,0,100,100,"reset");
	_btnReset->action += Delegate<DKOClientApp, int>(this, &DKOClientApp::reset);
	_btnReset->enableDraw = false;
	//
	////////////////////////////////////////////////////////
	
	////////////////////////////////////////////////////////
	// SETUP PRIORITY BUTTON
	////////////////////////////////////////////////////////
	//
	_btnPriority = new Button(ofGetWidth()-50,ofGetHeight()-50,50,50,"priority");
	_btnPriority->action += Delegate<DKOClientApp, int>(this, &DKOClientApp::priority);
	_btnPriority->enableDraw = false;
	//
	////////////////////////////////////////////////////////
	
	
	////////////////////////////////////////////////////////
	// SETUP GUI CLASS
	////////////////////////////////////////////////////////
	//
	_GUI.pushElement(_btnStartPlaying);
	_GUI.pushElement(_btnReselectPosition);
	_GUI.pushElement(_btnStopRecording);
	_GUI.pushElement(_btnSaveYourStory);
	_GUI.pushElement(_btnRecordAgain);
	//
	_GUI.pushElement(_btnReset);
	_GUI.pushElement(_btnPriority);
	//
	_GUI.setSleepTimeout(60);
	//
	////////////////////////////////////////////////////////
	
	
	////////////////////////////////////////////////////////
	// LOAD SCREEN GRAPHICS
	////////////////////////////////////////////////////////
	//
	_vecScreenImages.resize(NSCREENS);
	//
	_vecScreenImages[0].loadImage("1_main.png");
	_vecScreenImages[1].loadImage("2_countdown.png");
	_vecScreenImages[2].loadImage("3_recording.png");
	_vecScreenImages[3].loadImage("4_save.png");
	//
	_countdown.setup();
	_progress.setup(RECORD_SCREEN_INDEX);
	//
	////////////////////////////////////////////////////////
	
	
	//////////////////////////////////////
	// SOUND
	//////////////////////////////////////
	//
	sndCountdown.loadSound("countdown.wav");
	sndCountdown.setVolume(1.0f);
	sndReview.loadSound("review.wav");
	sndReview.setVolume(1.0f);
	//
	//////////////////////////////////////
	
	
	_dampScreen = 0;
	ofEnableAlphaBlending();
	reset();
}

//--------------------------------------------------------------
void DKOClientApp::update(){
	
	if (ofGetFrameNum() == 0)
		return;
	
	if (ofGetFrameNum() == 1)
	{
		longLoad();
		return;
	}
	
	//	if (_keyboard->getText() != serverHost)
	//	{
	//		serverHost = _keyboard->getText();
	//		weConnected = tcpClient.setup(serverHost, 5555);
	//		connectTime = ofGetElapsedTimeMillis();
	//	}
	
	////////////////////////////////////////////////////////
	// GUI
	////////////////////////////////////////////////////////
	//
	_GUI.update();
	
	_btnStartPlaying->enabled = (_screen==0);	
	_btnReselectPosition->enabled = (_screen==0);
	
	_btnSaveYourStory->enabled = (_screen==REVIEW_SCREEN_INDEX);
	_btnRecordAgain->enabled = (_screen==REVIEW_SCREEN_INDEX);
	
	_btnStopRecording->enabled = (_screen==RECORD_SCREEN_INDEX);
	//
	////////////////////////////////////////////////////////
	
	checkMessages();

	_screen %= NSCREENS;
	_dampScreen += (_screen - _dampScreen) * 0.2;
	
	
	//udate the countdown
	_countdown.update();
	if (_countdown.completed && _screen==COUNTDOWN_SCREEN_INDEX)
	{
		_screen++;
		_progress.progress=0;
		
		//send message to start recording
		ofxOscMessage msg;
		msg.setAddress("/action");
		msg.addIntArg(2);
		_oscSender->sendMessage(msg);
	}
}

//--------------------------------------------------------------

void DKOClientApp::draw(){

	if (ofGetFrameNum() == 0)
	{
		ofPushMatrix();
		ofScale(2.0, 2.0);
		drawAddresses();
		ofPopMatrix();
		return;
	}
	
	ofPushMatrix();
	
	//we translate the rendering
	//but not the touches
	ofTranslate(-_dampScreen * ofGetWidth(), 0, 0);
	
	//draw screens
	for (int i=0; i<NSCREENS; i++)
		_vecScreenImages[i].draw(ofGetWidth()*i,0);
	
	//draw countdown
	ofPushMatrix();
	ofTranslate(ofGetWidth()*COUNTDOWN_SCREEN_INDEX, 0,0);
	_countdown.draw();
	ofPopMatrix();
	
	//draw progressbar
	_progress.draw();

	ofPopMatrix();
	
	//////////////////////////////////////
	// DRAW GUI
	//////////////////////////////////////
	//
	_GUI.draw();
	//
	//////////////////////////////////////
	
	if (isPriority)
		ofDrawBitmapString("PRIORITY" , ofGetWidth() - 100, ofGetHeight() - 5);
}


void DKOClientApp::drawAddresses()
{
	vector<string> strAddresses;
	
	IPAddress::InitAddresses();
	IPAddress::GetIPAddresses();
	for (int i=0; i<MAXADDRS; ++i)
	{
		static unsigned long localHost = 0x7F000001;            // 127.0.0.1
		unsigned long theAddr;
		
		theAddr = IPAddress::ip_addrs[i];
		
		if (theAddr == 0) break;
		if (theAddr == localHost) continue;
		
		
		strAddresses.push_back(string(IPAddress::ip_names[i]));
//		NSLog(@"Name: %s MAC: %s IP: %s\n", if_names[i], hw_addrs[i],);
	}
	
	ofDrawBitmapString("loading", 10, 10);
	
	for (int i=0; i<strAddresses.size(); i++)
		ofDrawBitmapString("IP[" + ofToString(i) + "]: " + strAddresses[i], 10, 30+ i*20);

}

void DKOClientApp::advanceMode(const void* pSender, int &iAction)
{
	_screen++;
	_screen %= NSCREENS;
	
	if (_screen==1)
	{
		if (!newBoxSelected)
		{
			reselectScreen();
		}
		//send message to start indicating
		ofxOscMessage msg;
		msg.setAddress("/action");
		msg.addIntArg(MSG_INDICATE);
		_oscSender->sendMessage(msg);
	}
	
	if (_screen==COUNTDOWN_SCREEN_INDEX)
	{
		_countdown.reset();
		sndCountdown.play();
	}
}

void DKOClientApp::reselectScreen()
{
	ofxOscMessage msg;
	msg.setAddress("/action");
	msg.addIntArg(MSG_RESELECT);
	_oscSender->sendMessage(msg);
	
	newBoxSelected = true;
}

void DKOClientApp::reselectScreen(const void* pSender, int &iAction)
{
	ofxOscMessage msg;
	msg.setAddress("/action");
	msg.addIntArg(MSG_INDICATE);
	_oscSender->sendMessage(msg);
	reselectScreen();
}

void DKOClientApp::stopRecording(const void *pSender, int &iAction)
{
	ofxOscMessage msg;
	msg.setAddress("/action");
	msg.addIntArg(MSG_STOP);
	_oscSender->sendMessage(msg);
	_screen = REVIEW_SCREEN_INDEX;
	
}

void DKOClientApp::lastOptions(const void* pSender, int &iAction)
{
	if (iAction==0)
	{
		//save
		//send message to save
		ofxOscMessage msg;
		msg.setAddress("/action");
		msg.addIntArg(MSG_SAVE);
		_oscSender->sendMessage(msg);
		ofSleepMillis(100);
		reset();
		isPriority = false;
		
	} else if (iAction==1) {
		//record again
		
		_screen = 1;
		_countdown.reset();
		sndCountdown.play();		
		
		//send message to start indicator
		ofxOscMessage msg;
		msg.setAddress("/action");
		msg.addIntArg(MSG_RECORD_AGAIN);
		_oscSender->sendMessage(msg);
		
	} else if (iAction==2) {
		reset();
	} 

}

void DKOClientApp::reset()
{
	
	//send message to clear
	ofxOscMessage msg;
	msg.setAddress("/action");
	msg.addIntArg(MSG_CLEAR);
	_oscSender->sendMessage(msg);
	
	setPriority(false);
	
	sndCountdown.stop();
	sndReview.stop();
	
	newBoxSelected = false;
	
	_screen = 0;
}
void DKOClientApp::reset(const void* pSender, int &iAction)
{
	reset();
}

void DKOClientApp::priority(const void* pSender, int &iAction)
{
	setPriority(!isPriority);
}

void DKOClientApp::setPriority(bool b)
{
	isPriority = b;
	
	ofxOscMessage msg;
	msg.setAddress("/action");
	if (b)
		msg.addIntArg(MSG_SET_PRIORITY_1);
	else
		msg.addIntArg(MSG_SET_PRIORITY_0);
	_oscSender->sendMessage(msg);		
	
}

void DKOClientApp::checkMessages()
{
	bool isChangeSender=false;
	
	while (_oscReceiver.hasWaitingMessages()) {
		ofxOscMessage msg;
		_oscReceiver.getNextMessage(&msg);
		
		//progress update
		if (msg.getAddress()=="/progress")
			_progress.progress = msg.getArgAsFloat( 0 );
		
		//is finished
		if (msg.getAddress()=="/finished")
			if (msg.getArgAsInt32(0)==1)
			{
				if (_screen==RECORD_SCREEN_INDEX)
				{
					sndReview.play();
					_screen=REVIEW_SCREEN_INDEX;
					_progress.progress=1.0f;
				}
			}
		
		if (msg.getAddress()=="/sethost")
		{
			string newHost = msg.getArgAsString(0);
			if (newHost != _oscHost)
			{
				_oscHost = msg.getArgAsString(0);
				isChangeSender = true;
				ofLog(OF_LOG_VERBOSE, "OSC: setting host to " + _oscHost);
			}
		}
		if (msg.getAddress()=="/setsendport")
		{
			int newPort = msg.getArgAsInt32(0);
			if (_oscPortSend != newPort)
			{
				_oscPortSend = msg.getArgAsInt32(0);
				isChangeSender = true;
				ofLog(OF_LOG_VERBOSE, "OSC: setting send port to " + ofToString(_oscPortSend, 0));
			}
		}
	}
	
	if (isChangeSender)
	{
		delete _oscSender;
		_oscSender = new ofxOscSender();
		try {
			_oscSender->setup(_oscHost, _oscPortSend);
		}
		catch (char * exc) {
			ofLog(OF_LOG_ERROR, "osc setup: " + string(exc));
		}

	}
	
	
}

//--------------------------------------------------------------
void DKOClientApp::keyPressed  (int key){ 
	
}

//--------------------------------------------------------------
void DKOClientApp::keyReleased  (int key){ 
	
}

void DKOClientApp::touchDown(ofTouchEventArgs &touch){
	
	_dampScreen = _screen;
	
	_Touches[touch.id].isDown=true;
	_Touches[touch.id].setXY(touch.x,touch.y);
	_GUI.touchDown(&(_Touches[touch.id]));
	
	//_screen++;
}

//--------------------------------------------------------------
void DKOClientApp::touchMoved(ofTouchEventArgs &touch){
	_Touches[touch.id].moveXY(touch.x,touch.y);
    _GUI.touchMoved(&(_Touches[touch.id]));		
}

//--------------------------------------------------------------
void DKOClientApp::touchUp(ofTouchEventArgs &touch){
	
	_Touches[touch.id].isDown=false;
	_GUI.touchUp(&(_Touches[touch.id]));
	
}

//--------------------------------------------------------------
void DKOClientApp::touchDoubleTap(ofTouchEventArgs &touch){
	
}

//--------------------------------------------------------------
void DKOClientApp::lostFocus(){
	
}

//--------------------------------------------------------------
void DKOClientApp::gotFocus(){
	
}

//--------------------------------------------------------------
void DKOClientApp::gotMemoryWarning(){
	
}

//--------------------------------------------------------------
void DKOClientApp::deviceOrientationChanged(int newOrientation){
}


