#pragma once
#include "tinyxml2.h"

using namespace tinyxml2;

struct Node
{
	std::chrono::milliseconds time;
	int type;
	int pos;
};

class ContentLoader
{
public:
	void Load(std::vector<Node>& m_nodeList);

private:
	XMLDocument m_doc;
};

