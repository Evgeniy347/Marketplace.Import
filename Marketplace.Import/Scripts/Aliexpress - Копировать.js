
function MPS_Init() {
    //debugger;
     
    Date.prototype.addDays = function (days) {
        var date = new Date(this.valueOf());
        date.setDate(date.getDate() + days);
        return date;
    }
     
    if (!window.MPS_Aliexpress_Context)
        return; 

    if (location.href.startsWith("https://seller.aliexpress.ru/login")) {
        //если открывается страница авторизации то редиректимся на "login.aliexpress.com", так как там проще авторизаваться
        window.MPS_Aliexpress_Context.StartAuthorization = true;
        MPS_SaveContext();
        location.href = "https://aliexpress.ru";
    }
    else if (location.hostname == "login.aliexpress.com") {
        //Проходим авторизацию 
        setTimeout(MPS_Authorization, 500);
    } else if (location.hostname == "aliexpress.ru") {
        //Если это начало авторизации то редиректим на страницу входа
        if (window.MPS_Aliexpress_Context.StartAuthorization) {
            window.MPS_Aliexpress_Context.StartAuthorization = false;
            MPS_SaveContext();
            location.href = "https://login.aliexpress.com";
        }
        else {

            //Если мы тут то значит прошли авторизацию, редиректим на страницу для загрузки файла  
            location.href = "https://seller.aliexpress.ru/orders/orders";
        }
    } else if (location.href.startsWith("https://seller.aliexpress.ru/orders/orders")) {
        //открыть всплывающее меню действия 
        if (!window.MPS_Aliexpress_Context.StartCreateExport) {
            window.MPS_Aliexpress_Context.StartCreateExport = true;
            MPS_SaveContext();
            MPS_CreateExport();
        }
    }
    else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 100);
    }
}

function MPS_Redirect(url) {
    var value = "MPS_Redirect = " + url;
    console.log(value);
}

function MPS_CreateExport() { 

    var contextOperation = {};
    const request = new XMLHttpRequest();
    const url = "https://seller.aliexpress.ru/api/v1/order-export/create-export";
    var endDate = new Date();
    var startDate = endDate.addDays(-40);
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
            console.log("MPS_CreateExport");
            console.log(request.response);
            contextOperation.CreateExport = {
                Request: params,
                Responve: request.response,
            };
            contextOperation.order_export_id = request.response.data.order_export_id;
            MPS_CheckStatusExport(contextOperation);
        }
    });
    debugger;
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
                console.log("FileAliexpressUrl:" + order.file_url);
            }
            request.response.map
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

//Наживаем на кнопку "Действия"
function MPS_ClickAction() {

    var element = MPS_GetElementFilter({
        selector: "div",
        classNameContaince: "order-list_exportOrdersBtn_",
        childOptions: {
            selector: "button"
        }
    });

    if (!element) {
        setTimeout(MPS_Init, 100);
        return;
    }

    MPS_Aliexpress_Context.ButtonActionClick = true;
    element.click();

    setTimeout(MPS_ClickDownloadOrders, 100);
}

//Наживаем на кнопку "Выгрузить заказы"
function MPS_ClickDownloadOrders() {

    var element = MPS_GetElementFilter({
        selector: "li",
        classNameContaince: "action-list_action__",
        innerTextContaince: "Выгрузить заказы"
    });

    if (!element) {
        setTimeout(MPS_ClickDownloadOrders, 100);
        return;
    }

    MPS_Aliexpress_Context.LIActionClick = true;
    element.click();

    setTimeout(MPS_ClickSetDateValueReport, 100);
}

//Устанавливаем дату
function MPS_ClickSetDateValueReport(controlContext) {

    controlContext = controlContext ? controlContext : {};

    var element = controlContext.elementValue ? controlContext.elementValue :
        MPS_GetElementFilter({
            selector: "div",
            classNameContaince: "standart_modal_",
            childOptions: {
                selector: "#dateRange",
                classNameContaince: "input_input_"
            }
        });

    //Если дата установлена то очищаем ее
    if (element.value) {
        setTimeout(MPS_RemoveDateRangeReport, 100, controlContext);
        return;
    }

    //Открываем контрол выбора дат
    element.parentElement.parentElement.click();

    //получаем период формирования отчета
    if (!controlContext.InitDate) {
        var endDate = new Date();
        var startDate = endDate.addDays(-40);

        controlContext.countMonth = endDate.getMonth() - startDate.getMonth();
        controlContext.startDay = startDate.getDate();
        controlContext.endDay = endDate.getDate();
        controlContext.InitDate = true;
    }

    if (controlContext.countMonth > 0) {
        controlContext.countMonth--;

        var prevElement = controlContext.prevElement ? prevElement.prevElement :
            MPS_GetElementFilter({
                selector: "div",
                classNameContaince: "popover_content_",
                childOptions: {
                    selector: "span",
                    classNameContaince: "view-header_prev_"
                }
            });

        prevElement.click();
        setTimeout(MPS_ClickSetDateValueReport, 100, controlContext);
        return;
    }


    if (!controlContext.setStartDate) {

        var startElement = MPS_GetElementFilter({
            selector: "div",
            classNameContaince: "date-picker__border-radius_bottom-left",
            childOptions: {
                selector: "div",
                classNameContaince: "day_root_",
                innerTextContaince: "" + controlContext.startDay
            }
        });

        startElement = startElement ? startElement :
            MPS_GetElementFilter({
                selector: "div",
                classNameContaince: "date-picker_root_",
                childOptions: {
                    selector: "div",
                    classNameContaince: "day_root_",
                    innerTextContaince: "" + controlContext.startDay
                }
            });

        startElement.click();
        controlContext.setStartDate = true;

        setTimeout(MPS_ClickSetDateValueReport, 100, controlContext);
        return;
    }


    if (!controlContext.setEndDate) {

        var endElement = MPS_GetElementFilter({
            selector: "div",
            classNameContaince: "date-picker__border-radius_bottom-right",
            childOptions: {
                selector: "div",
                classNameContaince: "day_root_",
                innerTextContaince: "" + controlContext.endDay
            }
        });

        endElement.click();
        controlContext.setEndDate = true;

        setTimeout(MPS_ClickStartProcessReport, 100, controlContext);
        return;
    }
}


//очищаем дату
function MPS_RemoveDateRangeReport(controlContext) {

    var element = controlContext.RemoveContainer ? controlContext.RemoveContainer :
        MPS_GetElementFilter({
            selector: "div",
            classNameContaince: "standart_modal_",
            childOptions: {
                selector: "button",
                classNameContaince: "input-wrapper_clear_"
            }
        });

    if (!element || !element.click || element.disabled) {
        //Если кнопки удаления нет то открываем контрол выбора дат 
        setTimeout(MPS_ClickSetDateValueReport, 100, controlContext);
        return;
    }

    MPS_Aliexpress_Context.RemoveDateRange = true;
    element.click();
    setTimeout(MPS_RemoveDateRangeReport, 100, controlContext);
}

//Наживаем на кнопку "Выгрузить"
function MPS_ClickStartProcessReport() {

    var element = MPS_GetElementFilter({
        selector: "div",
        classNameContaince: "standart_modal_",
        childOptions: {
            selector: "button",
            innerTextContaince: "Выгрузить"
        }
    });


    if (!element || !element.click || element.disabled) {
        setTimeout(MPS_ClickStartProcessReport, 500);
        return;
    }

    var elementCount = MPS_GetElementFilter({
        selector: "div",
        classNameContaince: "standart_modal_",
        childOptions: {
            selector: "div",
            classNameContaince: "export-order-items_ordersNumber"
        }
    });

    debugger;

    if (!elementCount) {
        setTimeout(MPS_ClickStartProcessReport, 500);
        return;
    }


    var controlContext = {
        oldFile: MPS_GetCurrentFileReport(),
    };

    MPS_Aliexpress_Context.ClickStartProcessReport = true;
    element.click();

    setTimeout(MPS_ClickWaitProcessReport, 100, controlContext);
}

//Дожидаемся процесса выгрузки
function MPS_ClickWaitProcessReport(controlContext) {

    var element = MPS_GetElementFilter({
        selector: "div",
        classNameContaince: "standart_modal_",
        childOptions: {
            selector: "a",
            filter: function (x) { return x.download == "file.xlsx"; }
        }
    });

    if (controlContext.oldFile == element) {
        setTimeout(MPS_ClickWaitProcessReport, 100, controlContext);
    }
    else {
        setTimeout(MPS_GetFileLink, 100);
    }
}

function MPS_GetCurrentFileReport() {
    var element = MPS_GetElementFilter({
        selector: "div",
        classNameContaince: "standart_modal_",
        childOptions: {
            selector: "a",
            filter: function (x) { return x.download == "file.xlsx"; }
        }
    });
    return element;
}

//скачивание файла
function MPS_GetFileLink() {

    var link = document.querySelectorAll("a[download]")[0];
    var url = link.href;
    //console.log("FileAliexpressUrl:" + url);
}

//<span class="export-order-items_generationProgress__yfUuo text_root__1WqL3 text__variant_default__rE91W">Генерируем файл: 0%</span>

//function MPS_GetElementContainceClass(selector, containceClass) {
//    return MPS_GetElementsContainceClass(selector, containceClass)[0];
//}

//function MPS_GetElementsContainceClass(selector, containceClass) {
//    return MPS_GetElementsFilter(selector, function (x) { return x.className.indexOf(containceClass) != -1; });
//}

//function MPS_GetElementFilter(selector, filter, container) {
//    return MPS_GetElementsFilter(selector, filter, container)[0];
//}

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

//function MPS_GetElementsFilter(selector, filter, container) {
//    var t = {
//        type: "div",
//        className: "",
//        classNameContaince: "",
//        filter: function () { return true; },
//        child: {},
//    };

//    var elements = Array.from((container ? container : document).querySelectorAll(selector))
//        .filter(function (x) { return filter(x); });

//    return elements;
//}

function MPS_SaveContext() {
    if (window.MPS_Aliexpress_Context) {
        var value = "MPS_Aliexpress_Context = " + JSON.stringify(window.MPS_Aliexpress_Context);
        console.log(value);
    }
}

function MPS_Authorization() {

    var login = "ksemenova@mokeev.ru";
    var password = "H4TiWVS~|Z";

    var loginInput = document.querySelector("#fm-login-id");
    var pwdInput = document.querySelector("#fm-login-password");

    if (!loginInput || !pwdInput) {

        MPS_Aliexpress_Context.FindLoginAndPassorkInput = false;
        var checkVerifyLog = document.querySelector("#baxia-dialog-content");
        if (checkVerifyLog) {
            MPS_Aliexpress_Context.NeedCache = true;
            MPS_SaveContext();
            console.error("find baxia-dialog-content");
            console.error("Требуется ввод капчи, необходимо авторизоваться на сайте вручную.");
            //console.error("Application:Exit");
            return;
        };

        var buttonOK = document.querySelector(".comet-btn");
        if (buttonOK) {
            if (!buttonOK.AuthorizationClick) {
                MPS_Aliexpress_Context.AuthorizationDoubleClick = true;
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

    MPS_Aliexpress_Context.FindLoginAndPassorkInput = true;
    MPS_SaveContext();
    var buttonOK = document.querySelector(".login-submit");

    loginInput.focus();
    document.execCommand('insertText', false, login);

    pwdInput.focus();
    document.execCommand('insertText', false, password);
    MPS_Aliexpress_Context.ClickAutorize = true;
    MPS_SaveContext();
    buttonOK.click();
}


MPS_Init();