#include "ofMain.h"
#include "DKOClientApp.h"

int main(){
	ofSetupOpenGL(1024,768, OF_FULLSCREEN);			// <-------- setup the GL context

	ofRunApp(new DKOClientApp);
}
