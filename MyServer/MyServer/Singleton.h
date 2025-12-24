#pragma once

template<class T>
class Singleton
{
public:
	static T* Create()
	{
		if (instance == nullptr)
			instance = new T();

		return instance;
	}

	static T* Instance()
	{
		if (instance)
			return instance;

		return nullptr;
	}

protected:
	Singleton() {};
	virtual ~Singleton()
	{
		delete instance;
	}

	static T* instance;
};
template<class T>
_declspec(selectany) T* Singleton<T>::instance = nullptr;