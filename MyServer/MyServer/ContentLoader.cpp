#pragma once
#include <chrono>
#include <iostream>
#include <vector>

#include "ContentLoader.h"

void ContentLoader::Load(std::vector<Node>& m_nodeList)
{
	XMLError ec = m_doc.LoadFile("MusicNodeData.xml");
	if(ec != XML_SUCCESS)
	{
		std::cout << "Game Node Load Fail\n";
		return;
	}

	auto node = m_doc.FirstChildElement("AllNode")->FirstChildElement("Node");
	while(node)
	{
		Node newNode;
		double time = atof(node->FirstChildElement("Time")->GetText());
		newNode.time = std::chrono::milliseconds((long)(time * 1000));
		newNode.type = atoi(node->FirstChildElement("Type")->GetText());
		newNode.pos = atoi(node->FirstChildElement("Pos")->GetText());

		m_nodeList.push_back(newNode);

		node = node->NextSiblingElement();
	}

	std::cout << "List Size: " << m_nodeList.size() << "\n";
}
