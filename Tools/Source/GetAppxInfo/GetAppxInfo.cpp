// GetAppxInfo.cpp : Defines the entry point for the console application.
//

#include <iostream>
#include <string>

#include <Windows.h>

#include <wrl\client.h>
#include <wrl\wrappers\corewrappers.h>

#include <AppxPackaging.h>
#include <msopc.h>

#define RETURN_IF_FAILED(hr) do { HRESULT __hrRet = (hr); if (FAILED(__hrRet)) { return __hrRet; }} while (0, 0)

using namespace std;

using namespace Microsoft::WRL;
using namespace Microsoft::WRL::Wrappers;


HRESULT GetPackageIdFromAppxInternal(const wstring& packagePath)
{
    if (packagePath.length() == 0)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = CoInitializeEx(NULL, COINIT_MULTITHREADED);
    if (FAILED(hr))
    {
        wcout << L"CoInitializeEx failed" << endl;
        return hr;
    }

    ComPtr<IAppxManifestPackageId> appxManifestPackageId;
    ComPtr<IAppxManifestApplicationsEnumerator> appxApplications;
    ComPtr<IAppxFactory> appxFactory;
    ComPtr<IOpcFactory> opcFactory;
    ComPtr<IStream> packageStream;
    ComPtr<IAppxPackageReader> packageReader;
    ComPtr<IAppxManifestReader> manifestReader;

    RETURN_IF_FAILED(CoCreateInstance(__uuidof(AppxFactory), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(appxFactory.GetAddressOf())));
    RETURN_IF_FAILED(CoCreateInstance(__uuidof(OpcFactory), NULL, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(opcFactory.GetAddressOf())));
    RETURN_IF_FAILED(opcFactory->CreateStreamOnFile(packagePath.c_str(), OPC_STREAM_IO_READ, NULL, FILE_ATTRIBUTE_NORMAL, &packageStream));
    RETURN_IF_FAILED(appxFactory->CreatePackageReader(packageStream.Get(), &packageReader));
    RETURN_IF_FAILED(packageReader->GetManifest(&manifestReader));
    RETURN_IF_FAILED(manifestReader->GetPackageId(appxManifestPackageId.GetAddressOf()));
    RETURN_IF_FAILED(manifestReader->GetApplications(appxApplications.GetAddressOf()));

    ComPtr<IAppxManifestApplication> application;
    RETURN_IF_FAILED(appxApplications->GetCurrent(application.GetAddressOf()));

    LPWSTR value;
    
    RETURN_IF_FAILED(appxManifestPackageId->GetName(&value));
    wcout << L"Name              :" << value << endl;
    CoTaskMemFree(value);

    RETURN_IF_FAILED(appxManifestPackageId->GetPackageFamilyName(&value));
    wcout << L"PackageFamilyName :" << value << endl;
    CoTaskMemFree(value);

    RETURN_IF_FAILED(appxManifestPackageId->GetPackageFullName(&value));
    wcout << L"PackageFullName   :" << value << endl;
    CoTaskMemFree(value);

    RETURN_IF_FAILED(application->GetAppUserModelId(&value));
    wcout << L"AppUserModelId    :" << value << endl;
    CoTaskMemFree(value);

    return S_OK;
}

int wmain(int argc, wchar_t *argv[])
{
    if (argc < 2)
    {
        wcout << "GetPackageFullName <path to appx file>" << endl;
        return -1;
    }

    HRESULT hr = GetPackageIdFromAppxInternal(argv[1]);
    if (FAILED(hr))
    {
        wcout << L"ERROR: " << std::hex << hr << endl;
        return hr;
    }
    return 0;
}

