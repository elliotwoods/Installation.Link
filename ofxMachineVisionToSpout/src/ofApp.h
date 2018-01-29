#pragma once

#include "ofMain.h"
#include "ofxCvGui.h"
#include "ofxSpout.h"
#include "ofxMachineVision.h"
#include "ofxCvMin.h"
#include "ofxOsc.h"



class ofApp : public ofBaseApp{

	public:
		void setup();
		void update();
		void draw();
		void exit() override;

		void keyPressed(int key);
		void keyReleased(int key);
		void mouseMoved(int x, int y );
		void mouseDragged(int x, int y, int button);
		void mousePressed(int x, int y, int button);
		void mouseReleased(int x, int y, int button);
		void mouseEntered(int x, int y);
		void mouseExited(int x, int y);
		void windowResized(int w, int h);
		void dragEvent(ofDragInfo dragInfo);
		void gotMessage(ofMessage msg);

		ofxCvGui::Builder gui;
		ofxMachineVision::Grabber::Simple grabber;

		cv::CascadeClassifier faceCascade;

		thread faceTrackingThread;
		bool threadRunning = true;
		ofThreadChannel<ofPixels> incomingCameraFrames;
		ofThreadChannel<vector<ofRectangle>> incomingFaces;
		vector<ofRectangle> faces;

		ofRectangle cropRectangle;
		ofFbo cropped;

		ofxSpout::Sender spoutSender;

		ofxOscReceiver oscReceiver;
		chrono::system_clock::time_point lastMessageReceived = chrono::system_clock::now();
		bool isRecording = false;

		ofImage sharpened;

		struct : ofParameterGroup {
			struct : ofParameterGroup {
				ofParameter<float> radius{ "Radius", 2, 0, 6 };
				PARAM_DECLARE("Radius", radius);
			} sharpening;
			PARAM_DECLARE("Post processing", sharpening);
		} postProcessing;
};