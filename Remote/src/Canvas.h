#pragma once

#include "ofxKCTouchGui.h"
#include "ofxHomography.h"

#include "Connection.h"
#include "Quad.h"
#include "TypeSelection.h"

class ProjectorSelection : public ofxKCTouchGui::Elements::MultiSelect { };

class Canvas : public ofxKCTouchGui::Elements::Canvas {
public:
	typedef map<int, shared_ptr<Quad> > QuadMap;
	Canvas();
	void setConnection(shared_ptr<Connection>);
	void setProjectorSelection(shared_ptr<ProjectorSelection>);
	void setTypeSelection(shared_ptr<TypeSelection>);
	
	void refresh();
	
	void addNew();
	void deleteSelection();
	void bringSelectionToFront();
	void sendSelectionToBack();
	
	void flipHorizontal();
	void flipVertical();
	void rotateRight();
	void snapPoint();
	void resetQuad();
	
	shared_ptr<Quad> getSelection();
	void setSelection(int index);
	void setSelection(shared_ptr<Quad>);
	void clearSelection();
	
	ofVec2f getSelectedCornerLocation() const;
	ofMatrix4x4 getViewTransform() const;
	
	int getNextFreeIndex() const;
	int getNewBottomIndex() const;
protected:
	void setQuadToScreen(shared_ptr<Quad>);
	void draw();
	void drawWorkspace();
	
	void touchHit(ofxKCTouchGui::Touch &);
	void touchMoved(ofxKCTouchGui::Touch &) override;
	
	void callbackProjectorSelect(int &);
	void callbackTypeSelect(int & index);
	
	void updateSelectionOnServer();
	
	shared_ptr<Connection> connection;
	shared_ptr<ProjectorSelection> projectorSelection;
	shared_ptr<TypeSelection> typeSelection;
	
	QuadMap quads; // quads stored by id
	
	shared_ptr<Quad> selection;
	int selectedCorner;
	bool enableUpdateQuad;
};
