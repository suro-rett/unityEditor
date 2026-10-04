using Mono.Cecil.Cil;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;


//スクリプトに外からコードを追加する拡張エディター
public class AddCodeEditor : EditorWindow
{
    //ソースコードを解析・追加するクラス
    private CodeAnalysis codeAnalysis = new CodeAnalysis();

    //追加するCSファイルを入れているフォルダパス
    static string folderPath = "Assets/Scripts";

    //選択しているスクリプトファイル
    static int selectedFile = 0;

    //選択しているクラス
    static int selectedClass = 0;

    //選択しているRegion
    static int selectedRegion = 0;

    //スクリプトを検索する際、サブフォルダーも探すかどうか
    static bool subFolderCheck = true;

    //追加するコード
    string addCode;

    string analysisFilePath = null;

    //ToolsメニューからAdd Code Editorを開く
    [MenuItem("Tools/Add Code Editor")]
    public static void CreateWindow()
    {
        GetWindow<AddCodeEditor>("Add Code Editor");
    }

    //ウィンドウを開いたときに実行
    private void OnEnable()
    {

    }

   

    //GUI上の処理
    private void OnGUI()
    {
        EditorGUILayout.LabelField("編集したいファイルが入っているフォルダパスを入れて下さい");

        EditorGUI.BeginChangeCheck();

        //編集するスクリプト群が入っているフォルダを指定するフィールド
        folderPath = EditorGUILayout.TextField("フォルダパス",folderPath);

        if (EditorGUI.EndChangeCheck())
        {
            selectedFile = 0;
            selectedClass = 0;
            selectedRegion = 0;
        }

        //指定したフォルダが存在するか確認
        if (!Directory.Exists(folderPath))
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("指定したフォルダが存在しません");
            Debug.Log("指定したフォルダが存在しません");
            return;
        }

        GUILayout.Space(10);

        //スクリプトファイルの入れ物
        string[] csFiles;

        subFolderCheck = EditorGUILayout.Toggle("サブフォルダーも含めて検索", subFolderCheck);

        //指定フォルダ内の.csファイルを取得
        if (subFolderCheck)
        {
            //サブフォルダーの中のスクリプトも含めて検索
            csFiles = Directory.GetFiles(folderPath, "*.cs", SearchOption.AllDirectories);
        }
        else
        {
            //サブフォルダーの中のスクリプトを含めない
            csFiles = Directory.GetFiles(folderPath, "*.cs");
        }

        //CSファイルが存在するか確認
        if (csFiles.Length == 0)
        {
            EditorGUILayout.LabelField("CSファイルが存在しません");
            Debug.Log("CSファイルが存在しません");
            return;
        }

        //配列外を指してないかチェック
        if (selectedFile >= csFiles.Length)
        {
            selectedFile = 0;
            return;
        }

        //Popupに表示するファイル名の配列
        string[] fileNames = new string[csFiles.Length];

        //各ファイルのパスからファイル名だけ取得
        for (int i = 0; i < csFiles.Length; i++)
        {
            //フォルダパスからの相対ファイルパス取得
            fileNames[i] = Path.GetRelativePath(folderPath,csFiles[i]);
        }

        //どのCSファイルを変更するかの指定
        selectedFile = EditorGUILayout.Popup("変更するスクリプト", selectedFile, fileNames);


        GUILayout.Space(10);

        //ソースコードの解析
        AnalyzeFile(csFiles[selectedFile]);

        //クラス一覧を取る
        string[] csClass = codeAnalysis.ClassesName().ToArray();

        //クラスが存在するか確認
        if (csClass.Length == 0)
        {
            Debug.Log("クラスが存在しません");
            return;
        }

        //配列外を指してないかチェック
        if (selectedClass >= csClass.Length)
        {
            selectedClass = 0;
            return;
        }

        //変更するクラス設定
        selectedClass = EditorGUILayout.Popup("変更するクラス", selectedClass, csClass);

        GUILayout.Space(10);

        //Region一覧
        string[] csRegion = codeAnalysis.RegionsName(csClass[selectedClass]).ToArray();

        //配列外を指してないかチェック
        if (selectedRegion >= csRegion.Length)
        {
            selectedRegion = 0;
            return;
        }

        //どのRegion内に関数を入れるかまたはクラス内最後にいれるかを指定
        selectedRegion = EditorGUILayout.Popup("どこに関数を入れるか", selectedRegion, csRegion);

        GUILayout.Space(10);

        EditorGUILayout.LabelField("追加するコード");

        //コードを入力するテキストエリア
        addCode = EditorGUILayout.TextArea(addCode, GUILayout.MinHeight(200));

        if (GUILayout.Button("コード追加", GUILayout.Height(30)))
        {
            //現在のコードを指定している位置に追加する
            codeAnalysis.AddCode(selectedClass, selectedRegion, addCode);
        }
    }

    //ファイルの解析　解析していなかったら解析される
    //返り値は解析したらtrueを返す (解析成功・失敗ではない)
    bool AnalyzeFile(string filePath)
    {
        //ファイルの存在チェック
        if(!File.Exists(filePath)) { return false; }

        //既に解析済かをファイルパスが同じかで判定
        if(analysisFilePath == filePath) { return false;}
        
        //指定したクラスを基にスクリプトの解析
        bool check = codeAnalysis.SetClassesInfo(filePath);

        //選択しているクラスのリセット
        selectedClass = 0;

        //選択しているRegionのリセット
        selectedRegion = 0;

        //解析に失敗したためリセット
        if (!check)
        {
            selectedFile = 0;
            return false;
        }

        //解析したことを示すためanalysisFilePathに引数を入れる
        analysisFilePath = filePath;

        return true;
    }
}