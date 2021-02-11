#include "common.h"
#include <string>

#define VAR_COUNT 100

class BridgeGauge
{
private:
    HANDLE hSimConnect = 0;
    int m_variableCount = 0;
    int m_lastCommandId = 0;
    ID m_variableId[VAR_COUNT];

    static void s_DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData, void* pContext);
    void DispatchProc(SIMCONNECT_RECV* pData, DWORD cbData);

public:
    bool Initialize();
    bool OnFrameUpdate();
    bool Quit();
};

BridgeGauge GLOBAL_INSTANCE;

enum ClientData {
    WriteToSim = 0,
    ReadFromSim = 1,
};

struct WRITE_TO_SIM {
    char name[128];
    int index;
    int isSet;
    double value;
    int commandId;
};

struct READ_FROM_SIM {
    double value[100];
    int valueCount;
    int lastCommandId;
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

        SimConnect_MapClientDataNameToID(hSimConnect, "BRIDGE_ReadFromSim", ClientData::ReadFromSim);
        SimConnect_MapClientDataNameToID(hSimConnect, "BRIDGE_WriteToSim", ClientData::WriteToSim);

        SimConnect_CreateClientData(hSimConnect,
            ClientData::ReadFromSim,
            sizeof(READ_FROM_SIM),
            SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

        SimConnect_CreateClientData(hSimConnect,
            ClientData::WriteToSim,
            sizeof(WRITE_TO_SIM),
            SIMCONNECT_CREATE_CLIENT_DATA_FLAG_DEFAULT);

        SimConnect_AddToClientDataDefinition(hSimConnect, ClientData::ReadFromSim, 0, sizeof(READ_FROM_SIM), 0, 0);
        SimConnect_AddToClientDataDefinition(hSimConnect, ClientData::WriteToSim, 0, sizeof(WRITE_TO_SIM), 0, 0);

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

bool BridgeGauge::OnFrameUpdate()
{
    READ_FROM_SIM data;
    data.lastCommandId = m_lastCommandId;
    data.valueCount = m_variableCount;
    for (int i = 0; i < VAR_COUNT; i++) 
    {

        if (i >= m_variableCount) {
            data.value[i] = -99;
        }
        else
        {
            data.value[i] = get_named_variable_typed_value(m_variableId[i], get_units_enum("number"));

        }

    }

    SimConnect_SetClientData(hSimConnect,
        ClientData::ReadFromSim,
        ClientData::ReadFromSim,
        SIMCONNECT_CLIENT_DATA_SET_FLAG_DEFAULT,
        0,
        sizeof(READ_FROM_SIM),
        &data);
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
        GLOBAL_INSTANCE.OnFrameUpdate();
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_CLIENT_DATA)
    {
       // log("SimConnect Received Client data");

        auto recv_data = static_cast<SIMCONNECT_RECV_CLIENT_DATA*>(pData);
        auto data = (WRITE_TO_SIM*)(&recv_data->dwData);

        if (data->isSet == 1) 
        {
            m_variableId[data->index] = register_named_variable(data->name);

            set_named_variable_value(m_variableId[data->index], data->value);
        }
        else
        {
            m_variableCount = data->index + 1;
           log("Register " + std::string(data->name) + " " + std::to_string(m_variableCount));
            m_variableId[data->index] = register_named_variable(data->name);
            
        }

        m_lastCommandId = data->commandId;
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID::SIMCONNECT_RECV_ID_EXCEPTION)
    {
        SIMCONNECT_RECV_EXCEPTION* ex = static_cast<SIMCONNECT_RECV_EXCEPTION*>(pData);
        log("SimConnect EXCEPTION: " + std::to_string(ex->dwException));
    }
    else if (pData->dwID == SIMCONNECT_RECV_ID_OPEN)
    {
        log("SimConnect Open");
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