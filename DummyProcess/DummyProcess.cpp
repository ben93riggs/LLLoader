#include <Windows.h>
#include <iostream>

void hook_me()
{
	std::cout << "i sure hope no one hooks this function..." << std::endl;
}

int main()
{
	while (true) 
	{
		hook_me();
		Sleep(500);
	}
}