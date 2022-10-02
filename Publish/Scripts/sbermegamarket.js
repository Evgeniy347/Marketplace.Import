
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    if (location.href.startsWith("https://partner.sbermegamarket.ru/auth")) {
        if (!window.MPS_Context.StartAuth) {
            window.MPS_Context.StartAuth = true;
            MPS_PushLog("StartAuth");
            //Проходим авторизацию 
            setTimeout(MPS_Authorization, 1000);
        }
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/home")) {
        MPS_PushLog("RedirectRequest");
        setTimeout(function () { location.href = "https://partner.sbermegamarket.ru/reports/requests"; }, 1000);
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/reports/requests")) {
        if (!window.document.body.StartAuthorization) {
            window.document.body.StartAuthorization = true;
            MPS_CreateExport();
        }
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}

function MPS_CreateExport() {

    MPS_PushLog("StartCreateExport");

    if (MPS_SelectMerchant()) {
        return;
    }

    window.MPS_Context.sessionId = MPS_GetSesionID();

    if (!window.MPS_Context.sessionId) {
        setTimeout(MPS_CreateExport, 1000);
        return;
    }

    if (window.MPS_Context.StartCreateOrderReport)
        return;

    window.MPS_Context.StartCreateOrderReport = true;
    MPS_PushLog("StartCreateOrderReport");

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
                StartCreateDate: startDate.toStringMPS("yyyy-MM-dd"),
                EndCreateDate: endDate.toStringMPS("yyyy-MM-dd")
            },
            sessionId: window.MPS_Context.sessionId,
        }
    };

    MPS_PushLog("ReqestCreateExportStart");

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportCallback);
}

function MPS_SelectMerchant() {

    if (window.MPS_Context.ReqestSelectMerchantComplited)
        return false;

    var market = "EpilProfi Москва (со склада СберМегаМаркет)";

    var elementButton = MPS_GetElementFilter({
        selector: "span .goods-select__value-inner"
    });

    if (!elementButton) {
        MPS_PushLog("elementButton not found");
        setTimeout(MPS_SelectMerchant, 1000);
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
        MPS_PushLog("elementOption not found");
        setTimeout(MPS_SelectMerchant, 1000);
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

    MPS_PushLog("ReqestSelectMerchant");

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_SelectMerchantCallback);

    return true;
}

function MPS_SelectMerchantCallback(responce) {

    window.MPS_Context.ReqestSelectMerchantComplited = true;
    MPS_PushLog("ReqestSelectMerchantComplited");

    console.log(responce);
    setTimeout(MPS_CreateExport, 1000);
}

function MPS_CreateExportCallback(responce) {

    MPS_PushLog("ReqestCreateExportComplited");

    console.log(responce);
    if (responce.data.result)
        MPS_CheckStatusExport();
}

function MPS_CheckStatusExport() {

    MPS_PushLog("ReqestCheckStatusExport");

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

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CheckStatusExportCallBack);
}

function MPS_CheckStatusExportCallBack(responce) {

    MPS_PushLog("CheckStatusExportCallBack");

    console.log(responce);

    var order = responce.data.items[0];

    if (!order.isCanDownload) {
        //если сервер генерит отчет то проверяем его
        setTimeout(MPS_CheckStatusExport, 3000);
    }
    else {
        MPS_PushLog("FileExportComplited");
        var url = "https://partner.sbermegamarket.ru/api/market/v1/reportService/operationalReport/download?reportTaskId=" + order.reportTaskId + "&sessionId=" + window.MPS_Context.sessionId;
        console.log("FileReportUrl:" + url);
        console.log("StopAppScript");
    }
}

function MPS_GetSesionID() {
    var list = document.querySelectorAll(".requests-list__status");

    for (var i = 0; i < list.length; i++) {
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

        MPS_PushLog("FindLoginAndPassorkInput");
        setTimeout(MPS_Authorization, 100);
        return;
    }

    MPS_PushLog("FindLoginAndPassorkInput");

    var buttonOK = document.querySelector(".auth-form__submit-btn");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);

    MPS_PushLog("ClickAutorize");
    setTimeout(function () { buttonOK.click() }, 500);
}

MPS_Init();