#pragma once

#include "ofxKCTouchGui.h"
#include "ofxHomography.h"

#include "Connection.h"
#include "Quad.h"

class Canvas : public ofxKCTouchGui::Elements::Canvas {
public:
	typedef map<int, shared_ptr<Quad> > QuadMap;
	Canvas();
	void setConnection(shared_ptr<Connection>);
	
	void addNew();
	void deleteSelection();
	
	shared_ptr<Quad> getSelection();
	int getNextFreeIndex() const;
protected:
	void draw();
	void drawWorkspace();
	
	void touchHit(ofxKCTouchGui::Touch &);
	void touchMoved(ofxKCTouchGui::Touch &) override;
	
	void refresh();
	void updateSelectionOnServer() const;
	
	shared_ptr<Connection> connection;
	QuadMap quads; // quads stored by id
	
	shared_ptr<Quad> selection;
	int selectedCorner;
};
