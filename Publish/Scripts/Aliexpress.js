
function MPS_Init() { 

    if (!window.MPS_Context)
        return;

    if (location.href.startsWith("https://seller.aliexpress.ru/login")) {
        MPS_SaveContext();
        setTimeout(function () { location.href = "https://auth-seller.aliexpress.ru/api/v1/auth"; }, 1000);
    }
    else if (location.href.startsWith("https://login.aliexpress.ru")) {
        MPS_SaveContext();
        setTimeout(MPS_Authorization, 1000);
    }
    else if (location.href.startsWith("https://auth-seller.aliexpress.ru/api/v1/auth")) {
        MPS_SaveContext();
        setTimeout(function () { location.href = "https://seller.aliexpress.ru/orders/orders"; }, 1000);
    }
    else if (location.href.startsWith("https://seller.aliexpress.ru/orders/orders")) {

        setTimeout(function () { 
            if (!window.MPS_Context.StartCreateExport) {
                window.MPS_Context.StartCreateExport = true;
                MPS_SaveContext();
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

    contextOperation.CreateExport = {
        Request: params,
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportCallBack, contextOperation);
}

function MPS_CreateExportCallBack(response, contextOperation) {

    window.MPS_Context.ReqestCreateExportComplited = true;
    MPS_SaveContext();

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

    window.MPS_Context.MPS_CheckStatusExportCallBack = true;
    if (!window.MPS_Context.MPS_CheckStatusExportCount)
        window.MPS_Context.MPS_CheckStatusExportCount = 0;
    window.MPS_Context.MPS_CheckStatusExportCount++;
    MPS_SaveContext();

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
        window.MPS_Context.FileAliexpressComplited = true;
        MPS_SaveContext();
        console.log("FileReportUrl:" + order.file_url);
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

    MPS_Context.FindLoginAndPassorkInput = true;
    MPS_SaveContext();

    var buttonOK = document.querySelector("button[type=submit]");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);
    MPS_Context.ClickAutorize = true;
    MPS_SaveContext();
    buttonOK.click();
}

MPS_Init();