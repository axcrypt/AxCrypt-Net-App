//(function () {
//    'use strict';

//    angular
//        .module('app')
//        .controller('secretsView', secretsView);

//    secretsView.$inject = ['$location'];

//    function secretsView($location) {
//        /* jshint validthis:true */
//        var vm = this;
//        vm.title = 'secretsView';
//        activate();
//        function activate() { }
//    }
//})();

(function () {
    $('#secret-display-value').click(showSecret);
    $(document).click(function (e) {
        toggleHideForCardNumber(e);
        toggleHideForCardSecCode(e);
    });

    function toggleHideForCardNumber(e) {
        if ($(e.target).is('#secret-display-value, #secret-display-value *'))
            return;

        if ($(e.target).is('#copy-to-clipboard'))
            //CopyToClipboard();

        hidePassword();
    }

    function toggleHideForCardSecCode(e) {
        if ($(e.target).is('#card-exp-display-value, #card-exp-display-value *'))
            return;

        if ($(e.target).is('#card-exp-copy-to-cpbd'))
            //CardCopyToClipboard();

        hideCardSecCode();
    }

    $('#card-exp-display-value').click(showCardSecret);

    $('#secret-delete').click(function () {
        if (confirm('Are you sure you want to delete this password?'))
            $('#secret-delete-form').submit();
    });

    hidePassword();
    linkifyDescription();
    $('.secret-display').css('visibility', 'visible');

    hideCardSecCode();
    linkifyUrl();
    $('.secret1-display').css('visibility', 'visible');
});

function hidePassword() {
    $('#secret-display-value').html('*** Click to show ***');
}

function hideCardSecCode() {
    $('#card-exp-display-value').html('*** Click to show ***');
}

//function CopyToClipboard() {
//    showSecret();
//    document.execCommand("copy");
//}

//function CardCopyToClipboard() {
//    showCardSecret();
//    document.execCommand("copy");
//}

function showSecret() {
    $('#secret-display-value').html($('#secret-value').val());
    $('#secret-display-value').selectText();
}
function showCardSecret() {
    $('#card-exp-display-value').html($('#card-exp-sec-value').val());
    $('#card-exp-display-value').selectText();
}

function linkifyDescription() {
    var text = $('#secret-display-desc').html();
    var linkedText = Autolinker.link(text, { phone: false, email: false, twitter: false, hashtag: false });
    $('#secret-display-desc').html(linkedText);
}

function linkifyUrl() {
    var text = $('#secret-display-url').html();
    if (!text) {
        return;
    }
    var linkedText = Autolinker.link(text, { phone: false, email: false, twitter: false, hashtag: false });
    $('#secret-display-url').html(linkedText);
}