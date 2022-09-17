
function MPS_Init() { 

    if (!window.MPS_Context)
        return;

    if (location.href.startsWith("https://seller.aliexpress.ru/login")) {
        //если открывается страница авторизации то редиректимся на "login.aliexpress.com", так как там проще авторизаваться
        window.MPS_Context.StartAuthorization = true;
        MPS_SaveContext();
        location.href = "https://aliexpress.ru";
    }
    else if (location.hostname == "login.aliexpress.com") {
        //Проходим авторизацию 
        setTimeout(MPS_Authorization, 1000);
    } else if (location.hostname == "aliexpress.ru") {
        //Если это начало авторизации то редиректим на страницу входа
        if (window.MPS_Context.StartAuthorization) {
            window.MPS_Context.StartAuthorization = false;
            MPS_SaveContext();
            setTimeout(function () { location.href = "https://login.aliexpress.com"; }, 1000);
        }
        else {
            window.MPS_Context.EndAuthorization = true;
            MPS_SaveContext();

            //Если мы тут то значит прошли авторизацию, редиректим на страницу для загрузки файла   
            setTimeout(function () { location.href = "https://seller.aliexpress.ru/orders/orders"; }, 1000);
        }
    } else if (location.href.startsWith("https://seller.aliexpress.ru/orders/orders")) {
        //открыть всплывающее меню действия 
        if (!window.MPS_Context.StartCreateExport) {
            window.MPS_Context.StartCreateExport = true;
            MPS_SaveContext();
            MPS_CreateExport();
        }
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

    var login = "ksemenova@mokeev.ru";
    var password = "{Password:ksemenova@mokeev.ru}";

    var loginInput = document.querySelector("#fm-login-id");
    var pwdInput = document.querySelector("#fm-login-password");

    if (!loginInput || !pwdInput) {

        MPS_Context.FindLoginAndPassorkInput = false;
        var checkVerifyLog = document.querySelector("#baxia-dialog-content");
        if (checkVerifyLog) {
            MPS_Context.NeedCache = true;
            MPS_SaveContext();
            console.error("find baxia-dialog-content");
            console.error("Требуется ввод капчи, необходимо авторизоваться на сайте вручную.");
            //console.error("Application:Exit");
            return;
        };

        var buttonOK = document.querySelector(".comet-btn");
        if (buttonOK) {
            if (!buttonOK.AuthorizationClick) {
                MPS_Context.AuthorizationDoubleClick = true;
                MPS_SaveContext();
                buttonOK.click();
            }

            buttonOK.AuthorizationClick = true;
            setTimeout(MPS_Authorization, 100);
            return;
        }

        setTimeout(MPS_Authorization, 100);
        return;
    }

    MPS_Context.FindLoginAndPassorkInput = true;
    MPS_SaveContext();
    var buttonOK = document.querySelector(".login-submit");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);
    MPS_Context.ClickAutorize = true;
    MPS_SaveContext();
    buttonOK.click();
} 

MPS_Init();