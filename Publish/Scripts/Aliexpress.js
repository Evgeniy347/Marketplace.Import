
function MPS_Init() { 

    if (!window.MPS_Context)
        return;
      
    if (location.href.startsWith("https://seller.aliexpress.ru/login")) {  
        MPS_PushLog("Redirect auth");
        setTimeout(function () { location.href = "https://auth-seller.aliexpress.ru/api/v1/auth"; }, 1000);
    }
    else if (location.href.startsWith("https://login.aliexpress.ru")) { 
        setTimeout(MPS_Authorization, 1000);
    }
    else if (location.href.startsWith("https://auth-seller.aliexpress.ru/api/v1/auth")) {
        MPS_PushLog("Redirect orders");
        setTimeout(function () { location.href = "https://seller.aliexpress.ru/orders/orders"; }, 1000);
    }
    else if (location.href.startsWith("https://seller.aliexpress.ru/orders/orders")) {

        setTimeout(function () { 
            if (!window.document.body.StartAuthorization) {
                window.document.body.StartAuthorization = true;
                MPS_PushLog("StartCreateExport"); 
                MPS_CreateExport();
            }
        }, 1000); 
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}

function MPS_CreateExport() {

    var contextOperation = {};
    var url = "https://seller.aliexpress.ru/api/v1/order-export/create-export";
    var endDate = new Date();
    var startDate = endDate.addDays(-40); // 40 дней от текущей даты
    var params = {
        date_created_from: startDate.toISOString(),
        date_created_to: endDate.toISOString(),
        timezone: 5
    };
     
    MPS_SetEternalCookies();

    contextOperation.CreateExport = {
        Request: params,
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportCallBack, contextOperation);
}

function MPS_CreateExportCallBack(response, contextOperation) {
     
    MPS_PushLog("ReqestCreateExportComplited"); 

    console.log(response.response);
    contextOperation.CreateExport.Response = response;

    contextOperation.order_export_id = response.data.order_export_id;
    MPS_CheckStatusExport(contextOperation);
}


function MPS_CheckStatusExport(contextOperation) {

    contextOperation = contextOperation ? contextOperation : {};
    var request = new XMLHttpRequest();
    var url = "https://seller.aliexpress.ru/api/v1/order-export/get-export-list";
    var params =
    {
        page: 1,
        page_size: 5,
        order_export_statuses: ["Created", "Finished", "InProgress"]
    };
    contextOperation.CheckStatus = {
        Request: params,
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CheckStatusExportCallBack, contextOperation);
}

function MPS_CheckStatusExportCallBack(response, contextOperation) {
    console.log("MPS_CheckStatusExport");
    console.log(response);
     
    MPS_PushLog("MPS_CheckStatusExportCallBack"); 

    contextOperation.CheckStatus.Response = response;

    var order = null;

    if (contextOperation.order_export_id)
        order = MPS_GetOrder(response.data.order_exports, contextOperation.order_export_id);
    else
        order = response.data.order_exports[0];

    if (order.status != "Finished") {
        //если сервер генерит отчет то проверяем его каждую секунду
        setTimeout(MPS_CheckStatusExport, 3000, contextOperation);
    }
    else { 
        MPS_PushLog("FileAliexpressComplited"); 
        console.log("FileReportUrl:" + order.file_url);
        console.log("StopAppScript");
    }
}

function MPS_GetOrder(orders, id) {

    for (var i = 0; i < orders.length; i++) {
        var order = orders[i];
        if (order.order_export_id == id)
            return order;
    }
}


function MPS_Authorization() {
     
    var login = "{Login}";
    var password = "{Password}";

    var loginInput = document.querySelector("#email");
    var pwdInput = document.querySelector("#password");

    if (!loginInput || !pwdInput) {
        setTimeout(function () { location.href = "https://seller.aliexpress.ru/orders/orders"; }, 1000);
        return;
    }
     
    MPS_PushLog("FindLoginAndPassorkInput"); 

    var buttonOK = document.querySelector("button[type=submit]");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password); 
    MPS_PushLog("ClickAutorize");  
    setTimeout(function () { buttonOK.click() }, 500);
}

MPS_Init();