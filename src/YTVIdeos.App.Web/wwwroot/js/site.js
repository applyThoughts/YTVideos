// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

function getVideosForChannel() {
    if ($('#txtChannelName').val() == '') {
        alert('Please enter a valid youtube channel url or a video url');
        return false;
    }
    $('#main-container').loading();
    var data = {
        url: $('#txtChannelName').val()
    }
    $.ajax({
        cache: false,
        url: '/Home/GetChannelVideos',
        data: data,
        type: 'post',
        success: function (response) {
            $("div#videos-list").html(response.htmlData);
        },
        complete: function () {
            $('#main-container').loading('stop');
        }
    });
}
function playVideo(videId, title) {
    $("div#modal-youtube").find('h5.modal-title').html(title);
    $("div#modal-youtube").find('#channelVideo').attr('src', videId);
    $("div#modal-youtube").modal('show');
};
function downloadVideosCSV(url) {
    var a = document.createElement('a');
    a.href = url;
    a.download = url.split('/').pop();
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
}
$(document).ready(function () {
    $("#modal-youtube").on('hide.bs.modal', function () {
        $("#channelVideo").attr('src', '');
    });
});
