#include "common.h"
#include <string>

class BridgeGauge
{
private:
    HANDLE hSimConnect = 0;
    std::shared_ptr<std::string> m_str;

    static void CALLBACK s_DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);
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
    WriteToSimSize = 256,
    ReadFromSimSize = 8,
};

void CALLBACK BridgeGauge::s_DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext)
{
    GLOBAL_INSTANCE.DispatchProc(pData, cbData);
}

bool BridgeGauge::Initialize()
{
    if (SUCCEEDED(SimConnect_Open(&hSimConnect, "BridgeGauge", nullptr, 0, 0, 0)))
    {
        printf("### SimConnect connected.\r\n");

        SimConnect_MapClientDataNameToID(hSimConnect, "ReadFromSim", ClientData::ReadFromSim);
        SimConnect_MapClientDataNameToID(hSimConnect, "WriteToSim", ClientData::WriteToSim);

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

        printf("### SimConnect registrations complete.\r\n");
        return true;
    }
    else {
        printf("### SimConnect failed.\r\n");
    }

    return true;
}

bool BridgeGauge::OnFrameUpdate(double deltaTime)
{
    if (m_str != nullptr) {
        // printf("### OnUpdate %s\r\n", m_str->c_str());

        double outputVariable;
        execute_calculator_code(m_str->c_str(), &outputVariable, 0, 0);

        SimConnect_SetClientData(hSimConnect,
            ClientData::ReadFromSim,
            ClientData::ReadFromSim,
            SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
            0,
            ClientDataSize::ReadFromSimSize,
            &outputVariable);
    }

    return true;
}

bool BridgeGauge::Quit()
{
    return SUCCEEDED(SimConnect_Close(hSimConnect));
}

void BridgeGauge::DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData)
{
    printf("### SimConnect DispatchProc\r\n");

    if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EVENT)
    {
        SIMCONNECT_RECV_EVENT* evt = static_cast<SIMCONNECT_RECV_EVENT*>(pData);
        printf("### SimConnect EVENT\r\n");
    }

    if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_CLIENT_DATA)
    {
        printf("### SimConnect CLIENT_DATA\r\n");

        auto recv_data = static_cast<SIMCONNECT_RECV_CLIENT_DATA*>(pData);
        char* str = (char*)(&recv_data->dwData);
        m_str = std::make_shared<std::string>(str);

        printf("### SimConnect CLIENT_DATA: %s\r\n", str);
    }

    if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EXCEPTION)
    {
        SIMCONNECT_RECV_EXCEPTION* ex = static_cast<SIMCONNECT_RECV_EXCEPTION*>(pData);
        printf("### SimConnect EXCEPTION: %d \r\n", ex->dwException);
    }
}

extern "C" {
    MSFS_CALLBACK bool BridgeGauge_gauge_callback(FsContext ctx, int service_id, void* pData)
    {
        switch (service_id)
        {
        case PANEL_SERVICE_PRE_INSTALL:
            return true;
            break;
        case PANEL_SERVICE_POST_INSTALL:
            return GLOBAL_INSTANCE.Initialize();
            break;
        case PANEL_SERVICE_PRE_DRAW:
        {
            auto drawData = static_cast<sGaugeDrawData*>(pData);
            return GLOBAL_INSTANCE.OnFrameUpdate(drawData->dt);
        }
        break;
        case PANEL_SERVICE_PRE_KILL:
            GLOBAL_INSTANCE.Quit();
            return true;
            break;
        }
        return false;
    }
}
