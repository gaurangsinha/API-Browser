$(function () {
    $('form').submit(function () {

        var inputs = [];
        $(':input[type=text]', this).each(function () {
            inputs.push(this.name + '=' + escape(this.value));
        });

        var responseBox = $("#" + this.attributes['id'].value + '_response');

        //checkout http://jquery.com/api for more syntax and options on this method.
        jQuery.ajax({
            data: inputs.join('&'),
            url: this.action,
            type: this.method,
            error: function (jqXHR, textStatus, errorThrown) {
                //bg colour - #f6dbfc (Reddish)
                responseBox.attr('style', 'display: block');

                var hideButton = $("#" + responseBox.attr("id") + "_hider");
                hideButton.attr('style', 'display: inline');

                var urlBox = $("#" + responseBox.attr("id") + "_url");
                urlBox.html('<pre style="background-color: #f6dbfc;">' + this.url + '</pre>');

                var responseBody = $("#" + responseBox.attr("id") + "_body");
                responseBody.html('<pre style="background-color: #f6dbfc;"><code class="prettyprint"></code></pre>');
                $(responseBody[0].children[0].children[0]).text(jqXHR.responseText);
                prettyPrint();

                var responseHeader = $("#" + responseBox.attr("id") + "_header");
                responseHeader.html('<pre style="background-color: #f6dbfc;"><b>' + textStatus + ': ' + jqXHR.status + ' [' + errorThrown + ']</b></pre>');
            },
            success: function (data, status, xhr) {
                //bg colour - #fcf6db (Yellowish)
                responseBox.attr('style', 'display: block');

                var hideButton = $("#" + responseBox.attr("id") + "_hider");
                hideButton.attr('style', 'display: inline');

                var urlBox = $("#" + responseBox.attr("id") + "_url");
                urlBox.html('<pre>' + this.url + '</pre>');

                var responseBody = $("#" + responseBox.attr("id") + "_body");
                responseBody.html('<pre><code class="prettyprint"></code></pre>');
                $(responseBody[0].children[0].children[0]).text(xhr.responseText);
                prettyPrint();

                var responseHeader = $("#" + responseBox.attr("id") + "_header");
                responseHeader.html('<pre>Status: ' + xhr.status + ' [' + xhr.statusText + ']</pre>');
            }
        });

        //by default - we'll always return false so it doesn't redirect the user.
        return false;
    });
});