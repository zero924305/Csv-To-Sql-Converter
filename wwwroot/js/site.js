//replace file name
$('#File').change(function (e) {
    if (e.target.files.length) {
        $(this).next('.custom-file-label').html(e.target.files[0].name);
    }
});

//Copy sql query
$('#Copy').click(function () {
    $('#sqlQuery').select();
    $('#displayQueryData').select();
    document.execCommand('copy');
})

//Clear the changes in "Edit Import Query" textarea
$('#Clear').click(function () {
    $('#displayQueryData').val("");
})

//download convert data into sql file
$('#saveToFile').click(function (e) {
    var data = $("#displayQueryData").val();
    var data = 'data:text/plain;charset=utf-8,' + encodeURIComponent(data);
    var el = e.currentTarget;
    el.href = data;
    el.target = '_blank';
    el.download = 'CsvToSQL.sql';
});