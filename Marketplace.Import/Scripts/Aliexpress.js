
function MPS_Init() {
    //debugger;

    if (!Date.prototype.addDays) {
        Date.prototype.addDays = function (days) {
            var date = new Date(this.valueOf());
            date.setDate(date.getDate() + days);
            return date;
        }
    }

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
        setTimeout(MPS_Authorization, 500);
    } else if (location.hostname == "aliexpress.ru") {
        //Если это начало авторизации то редиректим на страницу входа
        if (window.MPS_Context.StartAuthorization) {
            window.MPS_Context.StartAuthorization = false;
            MPS_SaveContext();
            location.href = "https://login.aliexpress.com";
        }
        else {

            //Если мы тут то значит прошли авторизацию, редиректим на страницу для загрузки файла  
            location.href = "https://seller.aliexpress.ru/orders/orders";
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
    const request = new XMLHttpRequest();
    const url = "https://seller.aliexpress.ru/api/v1/order-export/create-export";
    var endDate = new Date(); 
    var startDate = endDate.addDays(-40); // 40 дней от текущей даты
    const params = {
        date_created_from: startDate.toISOString(),
        date_created_to: endDate.toISOString(),
        timezone: 5
    };

    request.responseType = "json";
    request.open("POST", url, true);
    request.setRequestHeader("Content-type", "application/json;charset=UTF-8");

    request.addEventListener("readystatechange", () => {

        if (request.readyState === 4 && request.status === 200) { 

            window.MPS_Context.ReqestCreateExportComplited = true;
            MPS_SaveContext();

            console.log(request.response);
            contextOperation.CreateExport = {
                Request: params,
                Responve: request.response,
            };
            contextOperation.order_export_id = request.response.data.order_export_id;
            MPS_CheckStatusExport(contextOperation);
        }
    });

    request.send(JSON.stringify(params));
}


function MPS_CheckStatusExport(contextOperation) {

    contextOperation = contextOperation ? contextOperation : {};
    const request = new XMLHttpRequest();
    const url = "https://seller.aliexpress.ru/api/v1/order-export/get-export-list";
    const params =
    {
        page: 1,
        page_size: 5,
        order_export_statuses: ["Created", "Finished", "InProgress"]
    };

    request.responseType = "json";
    request.open("POST", url, true);
    request.setRequestHeader("Content-type", "application/json;charset=UTF-8");

    request.addEventListener("readystatechange", () => {

        if (request.readyState === 4 && request.status === 200) {

            console.log("MPS_CheckStatusExport");
            console.log(request.response);
            contextOperation.CheckStatus = {
                Request: params,
                Responve: request.response,
            };

            var order = null;

            if (contextOperation.order_export_id)
                order = MPS_GetOrder(request.response.data.order_exports, contextOperation.order_export_id);
            else
                order = request.response.data.order_exports[0];

            if (order.status != "Finished") {
                //если сервер генерит отчет то проверяем его каждую секунду
                setTimeout(MPS_CheckStatusExport, 1000, contextOperation);
            }
            else {
                window.MPS_Context.FileAliexpressComplited = true;
                MPS_SaveContext();
                console.log("FileReportUrl:" + order.file_url);
            } 
        }
    });

    request.send(JSON.stringify(params));
}

function MPS_GetOrder(orders, id) {

    for (var i = 0; i < orders.length; i++) {
        var order = orders[i];
        if (order.order_export_id == id)
            return order;
    }
}

function MPS_SaveContext() {
    if (window.MPS_Context) {
        var value = "MPS_Context = " + JSON.stringify(window.MPS_Context);
        console.log(value);
    }
}

function MPS_Authorization() {

    var login = "ksemenova@mokeev.ru";
    var password = "H4TiWVS~|Z";

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