
function MPS_Init() {

    if (!window.MPS_Context)
        return;

    if (location.href.startsWith("https://partner.sbermegamarket.ru/auth")) {
        if (!window.MPS_Context.StartAuth) {
            window.MPS_Context.StartAuth = true;
            MPS_SaveContext(); 
            //Проходим авторизацию 
            setTimeout(MPS_Authorization, 1000);
        }
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/home")) { 
        window.MPS_Context.RedirectRequest = true;
        MPS_SaveContext();  
        setTimeout(function () { location.href = "https://partner.sbermegamarket.ru/reports/requests"; }, 1000); 
    }
    else if (location.href.startsWith("https://partner.sbermegamarket.ru/reports/requests")) {
        if (!window.MPS_Context.StartCreateExport) {
            MPS_CreateExport();
        }
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}

function MPS_CreateExport() {

    window.MPS_Context.StartCreateExport = true;
    MPS_SaveContext();

    var contextOperation = { sessionId: MPS_GetSesionID() };

    if (!contextOperation.sessionId) {
        setTimeout(MPS_CreateExport, 100);
        return;
    }

    if (MPS_SelectMerchant(contextOperation)) {
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
            sessionId: contextOperation.sessionId,
        }
    };

    contextOperation.CreateExport = {
        Request: params,
    };

    window.MPS_Context.ReqestCreateExportStart = true;
    MPS_SaveContext();

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateExportCallback, contextOperation);
}

function MPS_SelectMerchant(contextOperation) {

    if (window.MPS_Context.ReqestSelectMerchantComplited)
        return false;


    var market = "EpilProfi Москва (со склада СберМегаМаркет)";

    contextOperation = contextOperation ? contextOperation : { sessionId: MPS_GetSesionID() };

    var elementButton = MPS_GetElementFilter({
        selector: "span .goods-select__value-inner"
    });

    if (!elementButton)
        setTimeout(MPS_SelectMerchant, 100, contextOperation);

    if (elementButton.textContent == market)
        return false;

    var elementOption = MPS_GetElementFilter({
        selector: "select[name=select-merchant]",
        childOptions: {
            selector: "option",
            innerTextContaince: market
        }
    });

    var marketID = elementOption.value;
    var url = "https://partner.sbermegamarket.ru/api/market/v2/securityService/user/impersonate";
    var params = {
        meta: {
            from: "mui-router"
        },
        data: {
            merchantId: marketID,
            sessionId: contextOperation.sessionId
        }
    }

    window.MPS_Context.ReqestSelectMerchant = true;
    MPS_SaveContext();

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_SelectMerchantCallback, contextOperation);

    return true;
}

function MPS_SelectMerchantCallback(responce, contextOperation) {

    window.MPS_Context.ReqestSelectMerchantComplited = true;
    MPS_SaveContext();

    console.log(responce);
    setTimeout(MPS_CreateExport, 100);
}

function MPS_CreateExportCallback(responce, contextOperation) {

    window.MPS_Context.ReqestCreateExportComplited = true;
    MPS_SaveContext();

    console.log(responce);
    contextOperation.CreateExport.Response = responce;

    if (responce.data.result)
        MPS_CheckStatusExport(contextOperation);
}

function MPS_CheckStatusExport(contextOperation) {

    contextOperation = contextOperation ? contextOperation : { sessionId: MPS_GetSesionID() };

    window.MPS_Context.ReqestCheckStatusExport = true;
    if (!window.MPS_Context.ReqestCheckStatusExportCount)
        window.MPS_Context.ReqestCheckStatusExportCount = 0;
    window.MPS_Context.ReqestCheckStatusExportCount++;
    MPS_SaveContext();

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
            sessionId: contextOperation.sessionId,
        }
    }

    contextOperation.CheckStatus = {
        Request: params,
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CheckStatusExportCallBack, contextOperation);
}

function MPS_CheckStatusExportCallBack(responce, contextOperation) {

    window.MPS_Context.CheckStatusExportCallBack = true; 
    MPS_SaveContext();

    console.log(responce);
    contextOperation.CheckStatus.Response = responce;

    var order = responce.data.items[0];

    if (!order.isCanDownload) {
        //если сервер генерит отчет то проверяем его каждую секунду
        setTimeout(MPS_CheckStatusExport, 3000, contextOperation);
    }
    else {
        window.MPS_Context.FileExportComplited = true;
        MPS_SaveContext();
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

function MPS_CreateRestBuilder() {
    var request = new XMLHttpRequest();

    request.SendPost = function (url, json, callback, arg) {
        this.open("POST", url, true);
        this.responseType = "json";
        this.setRequestHeader("Content-type", "application/json;charset=UTF-8");
        this.mps_callback = callback;
        this.mps_callback_arg = arg;
        this.addEventListener("readystatechange", () => {

            if (request.readyState === 4 && request.status === 200) {
                if (request.mps_callback)
                    request.mps_callback(request.response, request.mps_callback_arg);
            }
        });
        this.send(JSON.stringify(json));
    }
    return request;
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
    var login = "aselivanova@mokeev.ru";
    var password = "{Password:aselivanova@mokeev.ru}";

    var loginInput = document.querySelector("input[name=login]");
    var pwdInput = document.querySelector("input[name=password]");

    if (!loginInput || !pwdInput) {

        MPS_Context.FindLoginAndPassorkInput = false;
        MPS_SaveContext();
        setTimeout(MPS_Authorization, 100);
        return;
    }

    MPS_Context.FindLoginAndPassorkInput = true;
    MPS_SaveContext();

    var buttonOK = document.querySelector(".auth-form__submit-btn");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);
    MPS_Context.ClickAutorize = true;
    MPS_SaveContext();
    setTimeout(function () { buttonOK.click() }, 500);
}


function MPS_GetElementFilter(options) {
    return MPS_GetElementsFilter(options)[0];
}

function MPS_GetElementsFilter(options) {
    //var options = {
    //    selector: "div",
    //    container: null,
    //    classNameContaince: null,
    //    filter: function () { return true; },
    //    childOptions: {},
    //};

    var container = (options.container ? options.container : document);
    var elements = Array.from(container.querySelectorAll(options.selector));

    if (options.classNameContaince)
        elements = elements.filter(function (x) { return x.className.indexOf(options.classNameContaince) != -1; });

    if (options.innerTextContaince)
        elements = elements.filter(function (x) { return x.innerText.indexOf(options.innerTextContaince) != -1; });

    if (options.filter)
        elements = elements.filter(options.filter);

    if (options.childOptions) {

        var result = [];
        for (var i = 0; i < elements.length; i++) {

            options.childOptions.container = elements[i];
            var resultChild = MPS_GetElementsFilter(options.childOptions);
            for (var j = 0; j < resultChild.length; j++) {
                result.push(resultChild[j]);
            }
        }

        return result;
    }
    else {
        return elements;
    }
}

MPS_Init();