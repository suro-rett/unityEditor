using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

//C#スクリプトを解析し、情報を保存およびコードの追加を行うクラス
public class CodeAnalysis
{
    //Classの範囲とそのメンバRegionを保持するためのクラス
    private class ClassInfo
    {
        //Classの名前
        public string Name;

        //Classの開始位置
        public int Start;

        //Classの終了位置
        public int End;

        //ClassのRegion群
        public List<RegionInfo> regionsInfo;
    }

    //Regionの範囲を保持するためのクラス
    private class RegionInfo
    {
        //Regionの名前
        public string Name;

        //Regionの開始位置
        public int Start;

        //Regionの終了位置
        public int End;
    }

    //ScriptのClassリスト
    private List<ClassInfo> classesInfo = new List<ClassInfo>();

    //解析、変更するCSファイルのパス
    private string filePath = null;

    //filePathのSyntaxTree
    private SyntaxTree tree = null;

    //SyntaxTreeのRootNode
    private SyntaxNode rootNode = null;

    #region SetClassInfo関数とそれの依存関数
    //引数を基に各クラスの情報をclassesInfo変数に入れる
    public bool SetClassesInfo(string algFilePath)
    {
        //指定されたスクリプトが存在するか確認
        if (!File.Exists(algFilePath))
        {
            //スクリプトが存在しないなら
            Debug.LogError("スクリプトが存在しない : " + algFilePath);

            ParameterReset();

            return false;
        }

        //C#スクリプトを読み込む
        string code = File.ReadAllText(algFilePath);

        //ファイルの存在確認が出来たため、メンバ変数filePathに引数のファイルパスを入れる
        filePath = algFilePath;

        //C#スクリプトのコードをSyntaxTreeに変換
        tree = CSharpSyntaxTree.ParseText(code);

        //SyntaxTreeのRootNodeを取得する
        rootNode = tree.GetRoot();

        //クラスを取得する
        var classes = rootNode.DescendantNodes().OfType<ClassDeclarationSyntax>();

        //classInfoを空にする
        classesInfo = new List<ClassInfo>();

        //クラスごとに処理
        foreach (var classNode in classes)
        {
            //クラスの情報をリストに入れる
            classesInfo.Add(SetClassInfo(classNode));
        }

        //スクリプト内にクラスが存在するか確認
        if (classesInfo.Count == 0)
        {
            //クラスが存在しないなら
            Debug.LogError("クラスが存在しない : " + filePath);

            ParameterReset();

            return false;
        }

        return true;
    }

    //変数を初期状態に
    private void ParameterReset()
    {
        //ScriptのClassリスト
        classesInfo = new List<ClassInfo>();

        //解析、変更するCSファイルのパス
        filePath = null;

        //filePathのSyntaxTree
        tree = null;

        //SyntaxTreeのRootNode
        rootNode = null;
    }

    //クラスのInfoを作り、各種データを入れてから返す関数
    private ClassInfo SetClassInfo(ClassDeclarationSyntax classNode)
    {
        //空のClassInfoを生成
        ClassInfo classInfo = new ClassInfo();

        //クラスの名前をセット
        classInfo.Name = classNode.Identifier.Text;

        //クラスのスタート位置をセット
        classInfo.Start = classNode.Span.Start;

        //クラスの終了位置をセット
        classInfo.End = classNode.CloseBraceToken.SpanStart;

        //クラスのRegionを取得・セット
        classInfo.regionsInfo = SetRegionInfo(classNode);

        //クラスの情報クラスを返す
        return classInfo;
    }

    //クラス内のRegionの開始位置と終了位置をセットする
    private List<RegionInfo> SetRegionInfo(ClassDeclarationSyntax classNode)
    {
        //対応したRegionの開始位置と終了位置を格納するリスト
        List<RegionInfo> regionRanges = new List<RegionInfo>();

        //regionRangesの0番目にはRegionが無かった時用の名前のみRegionInfoをいれる
        //またこれは外部からコード追加する際Regionの中ではなくクラスの一番下に追加する際にも使う
        regionRanges.Add(new RegionInfo());
        regionRanges[0].Name = "Regionなし";

        //現在開いているRegionの開始位置を管理するスタック
        Stack<int> regionStack = new Stack<int>();
        Stack<string> regionName = new Stack<string>();


        //クラス内のTriviaを順番に取得する
        foreach (var trivia in classNode.DescendantTrivia())
        {
            //regionを発見したら
            if (trivia.IsKind(SyntaxKind.RegionDirectiveTrivia))
            {
                //開始位置をスタックに保存する
                regionStack.Push(trivia.SpanStart);

                //Regionの名前をトリミングして設定
                regionName.Push(trivia.ToFullString().Replace("#region", "").Trim());
            }

            //endregionを発見したら
            else if (trivia.IsKind(SyntaxKind.EndRegionDirectiveTrivia))
            {
                //対応するregionが存在する場合
                if (regionStack.Count > 0)
                {
                    //名前や位置を保存するため空のRegionInfoを生成
                    RegionInfo range = new RegionInfo();

                    //Regionの名前を設定
                    range.Name = regionName.Pop();

                    //一番最後に開いたregionの開始位置を設定
                    range.Start = regionStack.Pop();

                    //Regionの終了位置を設定
                    range.End = trivia.SpanStart;

                    //Regionの範囲をリストに追加する
                    regionRanges.Add(range);
                }
            }
        }

        //RegionInfoを返す
        return regionRanges;
    }
    #endregion

    //各クラスの名前を返す
    public List<string> ClassesName()
    {
        //各クラスの名前の入れ物生成
        List<string> classesName = new List<string>();

        //各クラスごとに処理
        foreach(var classInfo in classesInfo)
        {
            //クラスの名前を追加
            classesName.Add(classInfo.Name);
        }

        return classesName;
    }

    //引数クラスの各Regionの名前を返す
    public List<string> RegionsName(string ClassName)
    {
        //各Regionの名前の入れ物生成
        List<string> regionsNames = new List<string>();

        //引数を基にクラスを特定する
        foreach (var classInfo in classesInfo)
        {
            //classInfoの名前と引数の名前がちがかったらcontinue
            if (classInfo.Name != ClassName) continue;

            //名前が合っていたらそのクラスの中の各Regionの名前を配列に入れる
            foreach (var regionInfo in classInfo.regionsInfo)
            { 
                regionsNames.Add(regionInfo.Name);
            }

            break;
        }

        //必ず一つはregionNameがあるはずなので、なかったら引数を間違えているため警告
        if(regionsNames.Count == 0)
        {
            Debug.Log("クラスが存在しません。引数合っていますか?");
        }

        return regionsNames;
    }

    //指定したクラスまたはレギオンの一番下にコードを追加する
    public void AddCode(int classNum,int regionNum,string code)
    {
        //クラスの範囲外チェック
        if(classNum >= classesInfo.Count)
        {
            Debug.Log("classNum引数が範囲外です");
            return;
        }

        //Regionの範囲外チェック
        if(regionNum >= classesInfo[classNum].regionsInfo.Count)
        {
            Debug.Log("regionNum引数が範囲外です");
            return;
        }

        //追加するコードが正しいC#コードか確認
        MemberDeclarationSyntax member =　SyntaxFactory.ParseMemberDeclaration(code);

        //構文エラーがある場合は追加しない
        if (member == null)
        {
            Debug.LogError("追加するコードに構文エラーがあります");
            return;
        }

        //SyntaxTreeから元のソースコードを取得する
        SourceText sourceText = tree.GetText();

        //インサート位置変数生成
        int insertPosition = 0;

        //インサート位置変更
        if (regionNum == 0)
        {
            //クラスの一番下にコードを差し込みたいため引数からどのクラスなのかを確認
            //引数と同じクラスを見つけたためインサート位置変更
            insertPosition = classesInfo[classNum].End;
        }
        else
        {
            //Regionの一番下にコードを差し込みたいため引数からどのクラスなのかを確認
            //クラスの中のRegionの終わり位置を引数から設定
            insertPosition = classesInfo[classNum].regionsInfo[regionNum].End;
        }

        //追加コードの前後に改行を入れる
        string insertCode = "\n" + code + "\n";

        //指定位置に追加する変更内容を作成する
        TextChange change = new TextChange(new TextSpan(insertPosition, 0), insertCode);

        //ソースコードに変更を適用する
        SourceText newSourceText = sourceText.WithChanges(change);

        //変更後のソースコードからSyntaxTreeを作り直す
        SyntaxTree newTree = CSharpSyntaxTree.ParseText(newSourceText);

        //変更後のコードを取得する
        string newCode = newTree.GetRoot().ToFullString();

        //ファイルへ保存する
        File.WriteAllText(filePath,newCode);


        Debug.Log(classesInfo[classNum].Name + "クラスの" + classesInfo[classNum].regionsInfo[regionNum].Name + "の中にコードを入れました");

        //コードの追加により位置がずれたり新たなRegionが出来た時用に再度解析しておく
        SetClassesInfo(filePath);
    }
}