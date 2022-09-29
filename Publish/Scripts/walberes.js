
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    if (!window.MPS_Context.PushLog)
        window.MPS_Context.PushLog = MPS_PushLog;

    if (location.href.startsWith("https://partner.sbermegamarket.ru/auth")) {
        if (!window.MPS_Context.StartAuth) {
            window.MPS_Context.StartAuth = true;
            window.MPS_Context.PushLog("StartAuth");
            //Проходим авторизацию 
            setTimeout(MPS_Authorization, 1000);
        }
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/home")) {
        window.MPS_Context.PushLog("RedirectRequest");
        setTimeout(function () { location.href = "https://partner.sbermegamarket.ru/reports/requests"; }, 1000);
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/reports/requests")) {
        if (!window.MPS_Context.StartCreateExport) {
            window.MPS_Context.StartCreateExport = true;
            MPS_CreateExport();
        }
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}


function MPS_CreateExport() {

    window.MPS_Context.PushLog("StartCreateExport");

    window.MPS_Context.sessionId = MPS_GetSesionID();

    if (!window.MPS_Context.sessionId) {
        setTimeout(MPS_CreateExport, 100);
        return;
    }

    if (MPS_SelectMerchant()) {
        return;
    }

    var url = "https://partner.sbermegamarket.ru/api/market/v1/reportService/operationalReport/generate";
    var endDate = new Date();
    var startDate = endDate.addDays(-40); // 40 дней от текущей даты
    var params =
    {
        meta: {
            from: "mui-reports"
        },
        data: {
            reportCode: "ORDER_REPORT",
            filters: {
                StartCreateDate: startDate.toStringMPS(),
                EndCreateDate: endDate.toStringMPS()
            },
            sessionId: window.MPS_Context.sessionId,
        }
    };

    var contextOperation = {};
    contextOperation.CreateExport = {
        Request: params,
    };

    window.MPS_Context.PushLog("ReqestCreateExportStart");

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportCallback, contextOperation);
}

function MPS_SelectMerchant(contextOperation) {

    if (window.MPS_Context.ReqestSelectMerchantComplited)
        return false;

    var market = "EpilProfi Москва (со склада СберМегаМаркет)";

    contextOperation = contextOperation ? contextOperation : {};

    var elementButton = MPS_GetElementFilter({
        selector: "span .goods-select__value-inner"
    });

    if (!elementButton) {
        window.MPS_Context.PushLog("elementButton not found");
        setTimeout(MPS_SelectMerchant, 100, contextOperation);
        return;
    }

    if (elementButton.textContent == market)
        return false;

    var elementOption = MPS_GetElementFilter({
        selector: "select[name=select-merchant]",
        childOptions: {
            selector: "option",
            innerTextContaince: market
        }
    });

    if (!elementOption) {
        window.MPS_Context.PushLog("elementOption not found");
        setTimeout(MPS_SelectMerchant, 100, contextOperation);
        return;
    }

    var marketID = elementOption.value;
    var url = "https://partner.sbermegamarket.ru/api/market/v2/securityService/user/impersonate";
    var params = {
        meta: {
            from: "mui-router"
        },
        data: {
            merchantId: marketID,
            sessionId: window.MPS_Context.sessionId
        }
    }

    window.MPS_Context.PushLog("ReqestSelectMerchant");

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_SelectMerchantCallback, contextOperation);

    return true;
}

function MPS_SelectMerchantCallback(responce, contextOperation) {

    window.MPS_Context.ReqestSelectMerchantComplited = true;
    window.MPS_Context.PushLog("ReqestSelectMerchantComplited");

    console.log(responce);
    setTimeout(MPS_CreateExport, 100);
}

function MPS_CreateExportCallback(responce, contextOperation) {

    window.MPS_Context.PushLog("ReqestCreateExportComplited");

    console.log(responce);
    contextOperation.CreateExport.Response = responce;

    if (responce.data.result)
        MPS_CheckStatusExport(contextOperation);
}

function MPS_CheckStatusExport(contextOperation) {

    contextOperation = contextOperation ? contextOperation : {};

    window.MPS_Context.PushLog("ReqestCheckStatusExport");

    const url = "https://partner.sbermegamarket.ru/api/market/v1/reportService/operationalReport/list";
    const params = {
        meta: {
            from:
                "mui-reports"
        },
        data: {
            limit: 10,
            offset: 0,
            order: "desc",
            sort: "requestDate",
            sessionId: window.MPS_Context.sessionId,
        }
    }

    contextOperation.CheckStatus = {
        Request: params,
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CheckStatusExportCallBack, contextOperation);
}

function MPS_CheckStatusExportCallBack(responce, contextOperation) {

    window.MPS_Context.PushLog("CheckStatusExportCallBack");

    console.log(responce);
    contextOperation.CheckStatus.Response = responce;

    var order = responce.data.items[0];

    if (!order.isCanDownload) {
        //если сервер генерит отчет то проверяем его каждую секунду
        setTimeout(MPS_CheckStatusExport, 3000, contextOperation);
    }
    else {
        window.MPS_Context.PushLog("FileExportComplited");
        var url = "https://partner.sbermegamarket.ru/api/market/v1/reportService/operationalReport/download?reportTaskId=" + order.reportTaskId + "&sessionId=" + contextOperation.sessionId;
        console.log("FileReportUrl:" + url);
    }
}

function MPS_GetSesionID() {
    var list = document.querySelectorAll(".requests-list__status");

    for (var i = 0; i < list.length; i++) {
        //href="/api/market/v1/reportService/operationalReport/download?reportTaskId=172341&sessionId=9FE56E41-5EE8-4DA4-B470-48FE221F077C"
        var link = list[i].querySelector("a");
        if (link) {
            var startIndex = link.href.indexOf("sessionId=");
            if (startIndex != -1) {
                var result = link.href.substring(startIndex + "sessionId=".length);
                var endIndex = result.indexOf("&");
                if (endIndex > -1)
                    result = result.substring(0, endIndex);

                return result;
            }
        }
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

    var loginInput = document.querySelector("input[name=login]");
    var pwdInput = document.querySelector("input[name=password]");

    if (!loginInput || !pwdInput) {

        window.MPS_Context.PushLog("FindLoginAndPassorkInput");
        setTimeout(MPS_Authorization, 100);
        return;
    }

    window.MPS_Context.PushLog("FindLoginAndPassorkInput");

    var buttonOK = document.querySelector(".auth-form__submit-btn");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);

    window.MPS_Context.PushLog("ClickAutorize");
    setTimeout(function () { buttonOK.click() }, 500);
}

MPS_Init();