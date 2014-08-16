#pragma once

#include "ofxKCTouchGui.h"
#include "ofxMySQL.h"

#include "Connection.h"

class TypeSelection : public ofxKCTouchGui::Elements::MultiSelect {
public:
	TypeSelection();
	void setConnection(shared_ptr<Connection>);
	void refresh();

	int getSelectionDatabaseIndex() const;
	string getCaptionForIndex(int index) const;
	void setSelectionByDatabaseIndex(int index);
	
	static TypeSelection & X();
	
	ofEvent<int> onTypeIndexSelection;
protected:
	void update();
	void callbackSelectCaption(string &);
	
	shared_ptr<Connection> connection;
	map<string, int> databaseID;
	static TypeSelection * singleton;
};