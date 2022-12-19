from tkinter.tix import COLUMN
import pandas as pd
import datetime
from datetime import date
import time
import shutil

path_to_archiv ='C:/Users/GTR/source/repos/Marketplace.Import/v/out2/Ozon_archiv_0.csv' # путь до архивного файла
path_to_current_file = 'C:/Users/GTR/source/repos/Marketplace.Import/v/out2/Ozon_current_0.csv' # путь до файла с текущими данными (которые будут перезаписываться)
path_to_file = 'C:/Users/GTR/source/repos/Marketplace.Import/v/out2/Ozon0.csv' # путь до объеденноного archiv и current файла

path_to_OneDrive_archiv = 'C:/Users/GTR/source/repos/Marketplace.Import/v/out/Ozon_archiv_0.csv'
path_to_OneDrive_current = 'C:/Users/GTR/source/repos/Marketplace.Import/v/out/Ozon_current_0.csv'
path_to_OneDrive_file = 'C:/Users/GTR/source/repos/Marketplace.Import/v/out/Ozon0.csv'
 
df_current_list = []
df_archiv_list = []
df_colums = COLUMN

def concat_unloading_to_archiv(payload):
    """Функция обрабатывает глобальные переменные df_current и df_archiv, добавляя к ним файл выгрузки ЮЛ. \nИмя файла, маркетпелйс и ЮЛ передается через справочник. \nПример {"file_unloading_name":"lamoda_lamoda_mokeev_au.csv", "market":"Lamoda"}"""
    global df_current_list
    global df_archiv_list
    global df_colums
    name = payload.get("file_unloading_name")
    start_time = time.time()
    print("start " + name)
    data = pd.read_csv(f"C:/Users/GTR/source/repos/Marketplace.Import/v/{name}", sep=";", encoding="utf-8")
    data.insert(0, "Маркетплейс", payload.get("market"))
    data.insert(0, "ЮЛ", payload.get("ЮЛ"))

    df_colums =  data.columns;
     
    for i in range(len(data)):
        item = data.loc[i]
        if datetime.datetime.strptime(item["Принят в обработку"], "%Y-%m-%d %H:%M:%S").date() >= (date.today() - datetime.timedelta(days=30)):
            df_current_list.append(item)
        else: 
            df_archiv_list.append(item) 
    print("--- %s seconds ---" % (time.time() - start_time))



concat_unloading_to_archiv({"file_unloading_name":"ozon_Ozon_101_lavant_report.csv", "market":"Озон", "ЮЛ":"ИП Мокеева А.С."})
concat_unloading_to_archiv({"file_unloading_name":"ozon_Ozon_101_likato_report.csv", "market":"Озон", "ЮЛ":"ИП Мокеев М.А."})
concat_unloading_to_archiv({"file_unloading_name":"ozon_Ozon_541_cosmo_report.csv", "market":"Озон", "ЮЛ":"ООО'"'Космо Бьюти'"'"})
concat_unloading_to_archiv({"file_unloading_name":"ozon_Ozon_541_EpilProfi_report.csv", "market":"Озон", "ЮЛ":"ИП Мокеев А.В."})

start_time = time.time()
print("save")
 
df_current = pd.DataFrame(df_current_list, columns=df_colums)
df_archiv = pd.DataFrame(df_archiv_list, columns=df_colums)

try:
    try:
             
            table_archiv_month = pd.read_csv(path_to_archiv,  sep="\t", encoding="utf-8")
            table_archiv_month = pd.concat([table_archiv_month, df_archiv], ignore_index=True)
            table_archiv_month = table_archiv_month.drop_duplicates().reset_index(drop=True)
            table_archiv_month.to_csv(path_to_archiv,  sep="\t", encoding="utf-8", index=False)
    except:
            df_archiv.to_csv(path_to_archiv,  sep="\t", encoding="utf-8", index=False)
    df_current.to_csv(path_to_current_file,  sep="\t", encoding="utf-8", index=False)

    try:# повторное удаление дубликатов, т.к. после записи в csv у добавленных строчек меняется формат данных и дубликаты не сбрасываются сразу
        table_archiv_month = pd.read_csv(path_to_archiv,  sep="\t", encoding="utf-8")
        table_archiv_month = table_archiv_month.drop_duplicates().reset_index(drop=True)
        table_archiv_month.to_csv(path_to_archiv,  sep="\t", encoding="utf-8", index=False)
    except:
        pass
except:
	print("Ошибка")

# Блок объединения файлов archiv и current - созданный для облегчения модели данных PBI
try:
    df_all = pd.concat([table_archiv_month, df_current], ignore_index=True)
    df_all = df_all.drop_duplicates().reset_index(drop=True)
    df_all.to_csv(path_to_file,  sep="\t", encoding="utf-8", index=False)
except:
    print("Ошибка")
##

try:
        shutil.copy2(path_to_archiv, path_to_OneDrive_archiv)
        shutil.copy2(path_to_current_file, path_to_OneDrive_current)
        shutil.copy2(path_to_file, path_to_OneDrive_file)
except:
        print("Ошибка копирования, OneDrive не доступен")

print("--- %s seconds ---" % (time.time() - start_time))