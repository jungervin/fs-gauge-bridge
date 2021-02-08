#include "common.h"
#include <string>

class BridgeGauge
{
private:
    HANDLE hSimConnect = 0;
    std::shared_ptr<std::string> m_str[31];
    int m_seq = 0;

    static void s_DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);
    void DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData);

public:
    bool Initialize();
    bool OnFrameUpdate(double deltaTime);
    bool Quit();
};

BridgeGauge GLOBAL_INSTANCE;

enum ClientData {
    WriteToSim = 0,
    ReadFromSim = 1,
};

enum ClientDataSize {
    WriteToSimSize = (256 * 31) + 4,
    ReadFromSimSize = (8 * 31) + 4,
};

struct WRITE_TO_SIM {
    char data1[256];
    char data2[256];
    char data3[256];
    char data4[256];
    char data5[256];
    char data6[256];
    char data7[256];
    char data8[256];
    char data9[256];
    char data10[256];
    char data11[256];
    char data12[256];
    char data13[256];
    char data14[256];
    char data15[256];
    char data16[256];
    char data17[256];
    char data18[256];
    char data19[256];
    char data20[256];
    char data21[256];
    char data22[256];
    char data23[256];
    char data24[256];
    char data25[256];
    char data26[256];
    char data27[256];
    char data28[256];
    char data29[256];
    char data30[256];
    char data31[256];
    int seq;
};

struct READ_FROM_WIM {
    double value[31];
    int seq;
};

void BridgeGauge::s_DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
    GLOBAL_INSTANCE.DispatchProc(pData, cbData);
}

enum ClientEvents
{
    EVENT_FRAME
};

bool BridgeGauge::Initialize()
{
    if (SUCCEEDED(SimConnect_Open(&hSimConnect, "BridgeGauge", nullptr, 0, 0, 0)))
    {
        log("SimConnect connected");

        SimConnect_MapClientDataNameToID(hSimConnect, "ReadFromSim2", ClientData::ReadFromSim);
        SimConnect_MapClientDataNameToID(hSimConnect, "WriteToSim2", ClientData::WriteToSim);

        SimConnect_CreateClientData(hSimConnect,
            ClientData::ReadFromSim,
            ClientDataSize::ReadFromSimSize,
            SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

        SimConnect_CreateClientData(hSimConnect,
            ClientData::WriteToSim,
            ClientDataSize::WriteToSimSize,
            SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

        SimConnect_AddToClientDataDefinition(hSimConnect, ClientData::ReadFromSim, 0, ClientDataSize::ReadFromSimSize, 0, 0);
        SimConnect_AddToClientDataDefinition(hSimConnect, ClientData::WriteToSim, 0, ClientDataSize::WriteToSimSize, 0, 0);

        SimConnect_CallDispatch(hSimConnect, s_DispatchProc, static_cast<BridgeGauge*>(this));

        SimConnect_RequestClientData(hSimConnect,
            ClientData::WriteToSim,
            0,
            0,
            SIMCONNECT_CLIENT_DATA_PERIOD_ON_SET,
            SIMCONNECT_CLIENT_DATA_REQUEST_FLAG_CHANGED,
            0,
            0,
            0);

        SimConnect_SubscribeToSystemEvent(hSimConnect, EVENT_FRAME, "Frame");

        log("SimConnect registration complete");
        return true;
    }
    else 
    {
        log("SimConnect_Open failed");
    }

    return true;
}

bool BridgeGauge::OnFrameUpdate(double deltaTime)
{
    if (m_str[0] != nullptr) {

        READ_FROM_WIM data;
        data.seq = m_seq;

        for (int i = 0; i < 31; i++)
        {
            execute_calculator_code(m_str[i]->c_str(), &data.value[i], 0, 0);
        }

        SimConnect_SetClientData(hSimConnect,
            ClientData::ReadFromSim,
            ClientData::ReadFromSim,
            SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
            0,
            ClientDataSize::ReadFromSimSize,
            &data);
    }

    return true;
}

bool BridgeGauge::Quit()
{
    return SUCCEEDED(SimConnect_Close(hSimConnect));
}

void BridgeGauge::DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData)
{
    if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EVENT)
    {
       // SIMCONNECT_RECV_EVENT* evt = static_cast<SIMCONNECT_RECV_EVENT*>(pData);
        log("SimConnect Event Id: %d" + std::to_string(pData->dwID));
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EVENT_FRAME)
    {
        GLOBAL_INSTANCE.OnFrameUpdate(0);
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_CLIENT_DATA)
    {
       // log("SimConnect Received Client data");

        auto recv_data = static_cast<SIMCONNECT_RECV_CLIENT_DATA*>(pData);
        auto data = (WRITE_TO_SIM*)(&recv_data->dwData);
        // char* str = (char*)(&recv_data->dwData);
        m_str[0] = std::make_shared<std::string>(data->data1);
        m_str[1] = std::make_shared<std::string>(data->data2);
        m_str[2] = std::make_shared<std::string>(data->data3);
        m_str[3] = std::make_shared<std::string>(data->data4);
        m_str[4] = std::make_shared<std::string>(data->data5);
        m_str[5] = std::make_shared<std::string>(data->data6);
        m_str[6] = std::make_shared<std::string>(data->data7);
        m_str[7] = std::make_shared<std::string>(data->data8);
        m_str[8] = std::make_shared<std::string>(data->data9);
        m_str[9] = std::make_shared<std::string>(data->data10);
        m_str[10] = std::make_shared<std::string>(data->data11);
        m_str[11] = std::make_shared<std::string>(data->data12);
        m_str[12] = std::make_shared<std::string>(data->data13);
        m_str[13] = std::make_shared<std::string>(data->data14);
        m_str[14] = std::make_shared<std::string>(data->data15);
        m_str[15] = std::make_shared<std::string>(data->data16);
        m_str[16] = std::make_shared<std::string>(data->data17);
        m_str[17] = std::make_shared<std::string>(data->data18);
        m_str[18] = std::make_shared<std::string>(data->data19);
        m_str[19] = std::make_shared<std::string>(data->data20);
        m_str[20] = std::make_shared<std::string>(data->data21);
        m_str[21] = std::make_shared<std::string>(data->data22);
        m_str[22] = std::make_shared<std::string>(data->data23);
        m_str[23] = std::make_shared<std::string>(data->data24);
        m_str[24] = std::make_shared<std::string>(data->data25);
        m_str[25] = std::make_shared<std::string>(data->data26);
        m_str[26] = std::make_shared<std::string>(data->data27);
        m_str[27] = std::make_shared<std::string>(data->data28);
        m_str[28] = std::make_shared<std::string>(data->data29);
        m_str[29] = std::make_shared<std::string>(data->data30);
        m_str[30] = std::make_shared<std::string>(data->data31);
        m_seq = data->seq;

       // log(m_str[0]->c_str());
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EXCEPTION)
    {
        SIMCONNECT_RECV_EXCEPTION* ex = static_cast<SIMCONNECT_RECV_EXCEPTION*>(pData);
        log("SimConnect EXCEPTION: " + std::to_string(ex->dwException));
    }
    else 
    {
        log("SimConnect Unknown DispatchProc Id: %d" + std::to_string(pData->dwID));
    }
}

extern "C" MSFS_CALLBACK void module_init(void)
{
    log("module_init");
    GLOBAL_INSTANCE.Initialize();
}

extern "C" MSFS_CALLBACK void module_deinit(void)
{
    log("module_deinit");
    GLOBAL_INSTANCE.Quit();
}