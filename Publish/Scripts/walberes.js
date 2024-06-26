﻿
function MPS_Init() {
    console.log("MPS_Init");

    if (!window.MPS_Context)
        return;

    MPS_SetCookie("locale", "ru");

    if (location.href.startsWith("https://seller.wildberries.ru/login")) {
        MPS_PushLog("StartAuth");

        if ("{ShowDevelop}" == "False") {
            console.log("Ждем аутентификацию");
        }
        else {
            console.log("Нужна аутентификация, закрываем скрипт");
            console.log("StopAppScript");
        }

    } else if (location.href.startsWith("https://seller.wildberries.ru/analytics")) {

        setTimeout(function () { MPS_CreateExport(); }, 800);
    } else {
        //если ничего из вышеперечисленного то вызывает повтроно, возможно будет редирект
        setTimeout(MPS_Init, 1000);
    }
}

function MPS_CreateExport() {

    if (!window.document || !window.document.body) {
        console.log("NotInitBody");
        return;
    }

    var startScript = window.document.body.StartScript;

    console.log("StartScript:" + startScript);

    if (startScript)
        return;

    window.document.body.StartScript = true;

    console.log("MPS_GetParams");
    var supplierid = MPS_GetParams("SupplierID");

    console.log("supplierid:" + supplierid);
    if (supplierid) {
        MPS_SetCookie("x-supplier-id", supplierid);
        MPS_SetCookie("x-supplier-id-external", supplierid);
    }

    console.log("setTimeout:MPS_CreateConsolidatedExport, MPS_CreateWeeklydynamicsExport");
    setTimeout(function () { MPS_CreateConsolidatedExport(); }, 1000);
    setTimeout(function () { MPS_CreateWeeklydynamicsExport(); }, 1500);
}

function MPS_CreateConsolidatedExport() {
    MPS_PushLog("MPS_CreateConsolidatedExport");

    var url =
        "https://seller.wildberries.ru/ns/consolidated/analytics-back/api/v1/consolidated-table-excel?isCommission=2";

    var params = {
        "filters": [
            "ordered", "paymentSalesRub", "paymentSalesPiece", "marginRate", "numberOrders",
            "goodsOrdered", "receiptsRub", "receiptsPiece", "turnover", "buybackPercentage",
            "logisticsCost", "storageCost", "surcharges", "salesRewards", "totalToTransfer"
        ]
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateConsolidatedCallback);
}

function MPS_CreateConsolidatedCallback(responce) {
    MPS_PushLog("MPS_CreateConsolidatedCallback");
    console.log(responce);

    MPS_DownloadBase64Data(responce.data.file, "Consolidated.XLSX", "application/octet-stream");

    MPS_UpdateLog(function (x) { x.ConsolidatedDownload = true; });
    MPS_CheckStopScript();
}

//****//

function MPS_CreateWeeklydynamicsExport() {
    MPS_PushLog("MPS_CreateWeeklydynamicsExport");

    var url = "https://seller.wildberries.ru/ns/weeklydynamics/analytics-back/api/v1/weekly-report-table-excel";

    var dateTo = new Date().addDays(1);
    var dateFrom = dateTo.addDays(-40); // 40 дней от текущей даты

    var params = {
        "brandID": 0,
        "contractID": -100,
        "officeID": -100,
        "dateFrom": dateFrom.toStringMPS("dd.MM.yy"),
        "dateTo": dateTo.toStringMPS("dd.MM.yy")
    };

    var request = MPS_CreateRestBuilder();
    request.SendPost(url, params, MPS_CreateWeeklydynamicsCallback);
}

function MPS_CreateWeeklydynamicsCallback(responce) {
    MPS_PushLog("MPS_CreateWeeklydynamicsCallback");
    console.log(responce);

    MPS_DownloadBase64Data(responce.data.excelReportWeeklyTable, "Weeklydynamics.XLSX", "application/octet-stream");
    MPS_UpdateLog(function (x) { x.WeeklydynamicsDownload = true; });
    MPS_CheckStopScript();
}

function MPS_CheckStopScript() {
    if (window.MPS_Context.WeeklydynamicsDownload && window.MPS_Context.ConsolidatedDownload)
        console.log("StopAppScript");
    else
        MPS_PushLog("MPS_CheckStopScript");
}

MPS_Init();