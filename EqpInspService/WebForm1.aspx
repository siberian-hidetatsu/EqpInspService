<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="EqpInspService.WebForm1" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title></title>
    <script src="./Scripts/jquery-3.4.1.min.js"></script>
</head>
<script>
    $(function () {
        // ［検索］ボタンクリックで検索開始
        $('#search').click(function () {
            // .phpファイルへのアクセス
            $.ajax('api/Employees',
                {
                    type: 'get',
                    dataType: 'json'
                }
            )
                // 検索成功時にはページに結果を反映
                .done(function (data) {
                    // 結果リストをクリア
                    //window.alert(data);
                    $('#result').empty();
                    // <Question>要素（個々の質問情報）を順番に処理
                    //$('Question', data).each(function() {
                    //  // <Url>（詳細ページ）、<Content>（質問本文）を基にリンクリストを生成
                    //  $('#result').append(
                    //    $('<li></li>').append(
                    //      $('<a></a>')
                    //        .attr({
                    //          href: $('Url', this).text(),
                    //          target: '_blank'
                    //        })
                    //        .text($('Content', this).text().substring(0, 255) + '...')
                    //    )
                    //  );
                    //});
                    for (var i in data) {
                        $("#result").append("<li>" + data[i].Name + "　" + data[i].BirthDay + "</li>");
                    }
                })
                // 検索失敗時には、その旨をダイアログ表示
                .fail(function (jqXHR, textStatus, errorThrown) {
                    //window.alert('正しい結果を得られませんでした。');
                    var result = "";
                    result += "jqXHR:" + jqXHR.status + " "; //例：404
                    result += "Status:" + textStatus + " "; //例：error
                    result += "error:" + errorThrown; //例：NOT FOUND
                    window.alert(result);
                });
        });
    });
</script>
<body>
    <form id="form1" runat="server">
  <div>
    <label for="keyword">キーワード：</label>
    <input id="keyword" type="text" size="20" />
    <input id="search" type="button" value="検索" />
  </div>
  <ul id="result" class="ajax"></ul>
    </form>
</body>
</html>
