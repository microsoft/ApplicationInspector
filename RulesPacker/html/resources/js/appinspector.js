$(document).ready(() => {
    // Initialize Bootstrap
    $('[data-toggle="popover"]').popover();
    $('[data-toggle="tooltip"]').tooltip();

    //embed image to make transportable
    var msLogoImage = "data:image/svg;base64,iVBORw0KGgoAAAANSUhEUgAAAIIAAACCCAIAAAAFYYeqAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAIKSURBVHhe7dMxSgNRFEbh1yip3YNLcAfuwG1Yp3cVdtrZiiCKg2DSpFLQSrAKgiaImDBvRmJSxImJoivwXDg/3wYuh5vyzmZI2xv5rpcn0/y192r2MOi0z9Pe5XpEZkAwA4IZEMyAYAYEMyCYAcEMCGZAMAOCGRDMgGAGBDMgmAHBDAhmQDADghkQzIBgBgQzIJgBwQwIZkAwA4IZEMyAYAYEMyCYAcEMCGZAMAOCGRDMgGAGBDMgmAHBDAhmQDADghkQzIBgBgQzIJgBwQwIZkAwA4IZEMyAYAYEMyCYAcEMCGZAMAOCGRDMgJAW90S0lfJN93eG+6di9yS1z0JK+bYX0nU3v77kqlpmqHL9Nh42JZqfiCjlyUdU3w2Wa0o0PxFUWh3h/nVmQCzN6hxUvTphtTLXZTUNKl09lhEV/XI4Kn9KNA3yeFg/FfVzJ6KUDkYh7Y8u+mXzE6sM1ax+LubHaX4aUlo7GkeUDkfNT/zJMOgsTipaEZkBwQwIZkAwA4IZEMyAYAYEMyCYAcEMCGZAMAOCGRDMgGAGBDMgmAHBDAhmQDADghkQzIBgBgQzIJgBwQwIZkAwA4IZEMyAYAYEMyCYAcEMCGZAMAOCGRDMgGAGBDMgmAHBDAhmQDADghkQzIBgBgQzIJgBwQwIZkAwA4IZEMyAYAYEMyCYAcEMCGZAMANA0foENh4CO77bRtMAAAAASUVORK5CYII=";

    $('#ms_logo').attr("src", msLogoImage);

    /* Main Navigation Links: These are links that change the
     * visible section to something else, specified by the
     * `data-target` attribute of the link, which should usually
     * be the #div-identifier to be shown.
     */
    $('a.nav-button').on('click', (e) => {
        let current = $('body').data('visible_section') || '#page__report_overview';
        let target = $(e.target).data('target');
        if (!!target) {
            if (!!current) {
                $(current).addClass('d-none');
            }

            $(target).removeClass('d-none');
            $('body').data('visible_section', target);
        }
    });

    $('a.file_listing').on('click', (e) => {
        $('#editor-container').addClass('d-none');
        $('#file_listing_modal').modal();
    })

    /*
     * When a user clicks on a file listing filename, load the data
     * to show into the Ace editor in the popup dialog.
     */
    $('#file_listing_modal').on('click', 'a.content-link', (e) => {
        const content = $(e.target).data('excerpt');
        const startLocationLine = $(e.target).data('startLocationLine');
        const endLocationLine = $(e.target).data('endLocationLine');
        const editor = ace.edit("editor");

        editor.setOption('firstLineNumber', startLocationLine);
        editor.getSession().setValue(content);
        editor.resize();
        editor.scrollToLine(0);
        editor.gotoLine(endLocationLine - startLocationLine + 1);

        $('editor-container').removeClass('d-none');
    });

    const templateInsertion = new TemplateInsertion(data);
    templateInsertion.processSummaryPage();
    templateInsertion.processProfilePage();
});

class TemplateInsertion {
    constructor(data) {
        this.mt = data.MetaData;
        this.md = this.mt.detailedMatchList;
    }

    processSummaryPage() {
        c3.generate({
            bindto: '#s_pi_analysis_chart',
            size: {
                width: 200
            },
            data: {
                columns: [
                    ['Analyzed', this.mt.filesAnalyzed],
                    ['Skipped', this.mt.filesSkipped],
                ],
                type: 'donut'
            },
            donut: {
                title: "Analyzed Files",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

        c3.generate({
            bindto: '#s_pi_patterns_chart',
            size: {
                width: 200
            },
            data: {
                columns: [
                    ['Unique Matches', this.mt.uniqueMatchesCount],
                    ['Repeats', this.mt.totalMatchesCount - this.mt.uniqueMatchesCount],
                ],
                type: 'donut'
            },
            donut: {
                title: "Results",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

        c3.generate({
            bindto: '#s_pi_languages_chart',
            size: {
                width: 200
            },
            data: {
                columns: Object.entries(this.mt.languages),
                type: 'donut'
            },
            donut: {
                title: "Source Types",
                label: {
                    format: function(value) { return value; }
                }
            }
        });

        $('#s_pi_application_name').html(this.mt.applicationName);
        $('#s_pi_version').html(this.mt.sourceVersion);
        $('#s_pi_description').html(this.mt.description || 'No description available.');
        $('#s_pi_source_path').html(this.mt.sourcePath);
        $('#s_pi_author').html(this.mt.authors || 'No author found.');
        $('#s_pi_date_scanned').html(this.mt.dateScanned);
    }

    combineConfidence(a, b) {
        if (a && !b) return a;
        if (b && !a) return b;
        if (!a && !b) return 'low';

        const _a = a.toLowerCase();
        const _b = b.toLowerCase();
        const map = { 'low': 1, 'medium': 2, 'high': 4 };
        if (map[_a] > map[_b]) { return a; }
        return b;
    }

    show_file_listing(e) {
        let $_tr = e.target.nodeName == 'TR' ? $(e.target) : $(e.target).closest('tr');
        let ruleId = $_tr.find('a').data('ruleId');
        let $this = e.data.obj;

        $('#file_listing_modal ul').empty();
        $('editor-container').addClass('d-none');

        const removePrefix = (fn) => {
            if (!fn.startsWith($this.mt.sourcePath)) {
                return fn;
            }
            return fn.slice($this.mt.sourcePath.length);
        };

        for (let match of $this.md) {
            let excerpt = (match.excerpt || '') || match.sample;
            if (match.ruleId === ruleId || match.ruleName === ruleId) {
                let $li = $('<li>');
                let $a = $('<a>');
                let $l = match.startLocationLine - 3;
                let $e = match.endLocationLine;
                if ($l <= 0) $l = 1; //fix #183
                $a.addClass('content-link')
                    .attr('href', '#')
                    .data('excerpt', excerpt)
                    .data('startLocationLine', $l)
                    .data('endLocationLine', $e)
                    .text(removePrefix(match.fileName));
                $li.append($a);
                $('#file_listing_modal ul').append($li);

                $('#match-line-number').text('Line number: ' + match.startLocationLine.toString());
            }
        }
        $('#file_listing_modal').on('shown.bs.modal', function (e) {
            $('a.content-link').first().trigger('click');
        });

        $('#file_listing_modal').modal();
    }

    /*
     * Builds the Profile page, including event handlers.
     */
    processProfilePage() {

        //embed images to make transportable
        var btnToggleNone = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAEEAAAA2CAYAAACY0PQ8AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAADBSURBVGhD7dqxCYBADEDRnEs4ndM5nVPoP7hCxCbNBfG/Ru3kYxCD7UT83DKOv2YEGAFGgBFQ+nZo+zHOIs5tHWfzlT0J9wDd83omxwFGgBFgBBgBRoARYAQYAUaAEWAEGAFGgBGQXqpUfvdnZJY0qSfhKwG6zL06DjACUhEql6FZmXst2za/zWxVZMcBRoARYAQYAUaAEWAEGAFGgBFgBBgBRoARYAQYAWURnlukytWdv/rDcYARYAQYAUaAESLiAh45KVR0Pg9xAAAAAElFTkSuQmCC";
        var highConfImage = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAADYAAAAeCAYAAABnuu2GAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsIAAA7CARUoSoAAAAXwSURBVFhH3ZhtTFNXGMf/lFdbBwVaFZRVM94TLWxKYmayTfygmZowssW9sAwSHDEs2RKVkE2BTJJl8MHNZRtmkG3VLbA1sgV1c8AAZUXooJUpKjitizgpHbfQFlIG3T2np7xV2ltAPuyXNLfnOTe9/d/nOc/znOPn4MH/kCUXZrPZ0NHRgcHBQTrW/aGl15kkJ2xEUGAwwsLCsHnzZkilUjazdCyJMI7joNVq0dHVhhs9NyFP9INo5Sideyxmgl5nYukXwTHhB8doCP65KYJMLsMzT6dTkXK5nN21OBYlzGg04osvK3Crrw8R8ZOQKGyIiHMX4o2Re37gbq4A1xuE1avW4PVXchAXF8dmF8aChJFwU9fWQNN+Cau3cQsSMx/muyIMtIZDERXPC8xesAd9FvZrUyOqv/8GMuUo5E/aIApgE0uMqccfA21SbE3bhn0vvorAwEA2IwyfhFXyYXdj4DKin+XgH7zopemVyX/5cO8UQ8wl4NDbhT6JEyRsfHwcpaWlmHy8F/KnbMy6fHB9wZjoTkR2djYUCgWzekbErvNCEsSxY+9g9+6vIbq7uAW9UIio8vgrePezD9Ch+51ZPePRY8RTR4/mIy/vO/5NDVFbcXERIjP09PtyYDqjREmq83lB437IaJfh/dxDXrOmR4+dOFGCzMxfpkQRiotL6MOWg5miCPZAB+pSBvHhFx/T2umJeYWp1V/xghr5onmbWaZZDnFzRbmwShz4MbYfpSfKaETNx0OFabUaGAxneG9pmMWdRyluPlEuBlZNol5yBydPVTGLO27CyFtQqU4iJ+dnZpkfIeJIwZ378YQ3US50T9hwUXeZd4CBWWbjljxICAIVHr01l7kJhftThOHelTBedyBGsZZZp7lz6y+sSgxAaLxlVtciVJSLdff8kTOSgpKC95hlmlnCyIIsKnoL5eXf8sXQtzbJJe72D1JIA2JoU7tlyxaIxWJ2xzQkKkjT3HSpAQ+424hOH4KtZZNPoly81BKJY3kFbllyljCV6nPIZJ9g166rzCKc3t5InD6dz9e73VAqlYK6BBJGVqsVNTU16Bt7AHXK3xhbwSYFEs6JsK97HT4t/4hZnEwFPGlsNZpW7NhxnVmEo9HEorr6NRw+fJhuPYS2Pmq1GmVlZYiMjMQbOzKpKInVj80KY0g6CZPDyr/YXmZxMiVMr9cjKWnQ5xAknmpoeB4FBWUPDTtPZGVl0bBsampCZWUlXq5egayLq1Fp2sPuEEavzIpOvY6NnEwJ02pJzephI2EYjRKcOpWB/PxinxpUF2RLsn37dgQEBKC2thYXLlxAa2sr9SQRST5CuBNtR3PbJTZyQoWRt9bD73yVyvvUKBSV6jns2fPmgrb2JPRVKhXq6+thsVgQFBQEk8mE7u5unDt3Dp2dnfToQAgkHM3WYdrXuqDCrl27xncZw3wo2alRCAZDOP9DMXRN+QoRc+DAAdTV1dHzESJkeHiYzTpZv349UlJS2Mg799ZM0OXkggrr47f2sbG3qEEoDQ2bkJ7+AhsJgyzwgwcPoqqqCjqdjoYfefZc/P39ERERgbw1PzGLd+6G2aC/MZ3NqTCz+T7vdufhi1D0+rU0rQuB1Mfjx4/zNbIIXV1dOHv2LH27drt7hISEhNAs6StjIQ4MDpnYiAnjOCO/TsaoQQgkDMXiMMHnEWazGe3t7dQ7jY2NbmFHCA8P5+vnLloHo6Ki6PraqheeZUmpMHNmNpryGOeTxzguhH8RwhY2gex6yZ/esGED9chMSNIgHcrOnTuRlpYGiUSCxMREur7C7MIz7WjwJGwjFjYC6FEMx1nQ1qbAlSvR1OgNg4Fvm6QyNhJGZmYmWlpakJycTDMeISEhgQoIDQ3F/v37aSI6f/48zZZEXGjXb9h4Vbg4+8i0c2hLRX6MpF9fIH9C6PmDi+bmZlRUVNBMmJSURAVlZGRg7969s+pgYWEhjhw5QkX6AmkQSGQQ3Lr7Rw3Jiv39/UhNTUVubu5DayCpR4s9EV52YUQUyZIkJB8dwH+dUJjrEP/0MwAAAABJRU5ErkJggg==";
        var medConfImage = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAADYAAAAeCAIAAADo2HrRAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAU6SURBVFhHzZd7TFtVHMcvLY9SHi2UjlegBkoLm2vnwCmviRBRAnOBTjTTzAB7JbBEky0LMcESRzBO/1B8bMskIGTRIZkyLSGyucUtvMoE3GDQ1lEmTGiBWx6l0NH6K+cwCrS9vBL3SUPP93dObr/87nn8jpPZbCaebjZlUa/Xt7W1abVaaHfclaMgYrtwp6uLG4vFiomJYbPZOLohNmKRJEm5XN72Z3NvTx830onmOQNBr5B51IuYGqKZ553MM4yxPpof1++l+BTwyuVycfd6WJ9FjUZzseK8Sqn0FZg8eHrfiGW27DE56ET2uZMKV/9tAYcO5kZEROCOtbFWi/BOa3+63NR6yz+BXKOz1egGaCO3fXiBgkMHc9ae0TVZ/P3G9R9+vOQnnuHu1tOccXDDjPbQR5rZsXsS3nrjbRcXFxy1D7XFbyvO9460BCWRdLd1z1p7mB4TmjtMJik89V4hpUtHFo1GY0lJiSlUwY3W49CWQird5v+KzMnJ4fF4OGQLGv5eBayMM2fez8j4jjawvtm9dsDfp4KuD775uK2jHYdsYTuLkL+iooLjx2t4vHGQUumHnMxO1LVVjF4RFz9neaar0Smz1e+jI6fsrXTbWSwrK5ZIfkP+AKm0GJ6I2lvCE3/AnIv5l13aTy5+AdstiqzAhsXa2koe73pMzAOsF9hCl9b+ENMe5jr+UEnZWXh7OGTFSotyeZNafUUiacLaii1xudofYmSbqdGj/0J1OdZWLLMI/0RV1YXc3AasV+HAJWzL8OEPHYUPNHB0Ofb8ITrC9X90tKjVaqwXWfasurpLe/f2stkGrG2xwiX5N22gwbv9c6/ZO4Jo58NfLgCN1s88+n9ljSnoeByVP8Rt4UT591VYLEKXSqWoBbO1svKrEyca6HSKLTop6Wb9uTeZUcMPfmYzxvipsZKjR47BbgzmZDKZQqEYHx/Pyzu8OyrhYdfMw/ZZ9wDDRAO1P2DC2+zfPbcrLIrD4eCQtcWamgqRqE4oHEbSMcHBd1suvy5Jf+dAVnZYWFhjYyPyh3qRS5FIlPNuXmhgRIdMqzQMK/ymHlOfdsQQa3ZKdj89NQ3rJxahSigv/zo//wZlCoGmJv7VqwdOniyCU4FOp9fX11v7QyCXvr6+cXFx8fHxeyLF2rouBWfK6IoH2MPAMIepna0TiS1C/Wc01sfGKlDUAQoFB/ydPn2WwWCABH8VFRUqlQqWmsGwNIl9fHzg7+DgoKenp1AohKp2xzOCQVnH/WC92fZaWsJ12hxu9Hl2+w4k8XC5HDbCHtR2gEbjUV2dWVAgtT77s7Oz8/Pzk5OTsV4AJAShC2uCgMMjKzb1hV4vrO3THzR3s/kWFsgiJKCnp08sfoRCDqiqennfvmPWhX5aWppEIklMTMTaCghCFwzAmiAyM/YL/nFjWIp0R4yzTbrpCSgSkLRY7O7u5vEmmMw5FLKHWu2j0YRAfY/1+oHcZ6Xvf15JncjBgPnOTrwDWCwqlUo+X4W0A65dE6WkZGGxUV575dVQFUGnKtsHWPrO3nuobbGo0z1isaiyTxCdncFi8WYPQEhkeKQg8N+lLd0msK6146OobbFIkhrHJwoAb5nJZG3sCreCF0XRIVo3LOxgcCd0pA61URZJyiySJIPNZmGxOWC1seYoNvEZN5N+cgq1LZclkpxqbuZ1dQWhkE3UaniyHxabA+7/3lrzznsULucmcdYsVTdsv3C6IO0AWMv2LhlQnhQWFmKxSGlpqc3x8Fvwi1jYh8lkog2L+gb4v2OZi081BPEf70xKp6czrYoAAAAASUVORK5CYII=";
        var lowConfImage = "data:image/gif;base64,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";
        var emptyConfImage = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAADYAAAAeCAYAAABnuu2GAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAAPDSURBVFhH3ZhfSFNRHMd/Rhb4sj04K0xGpCaCGeRD0kOBD2VtWPoWFv4jmcFMLP+lTpslGkpIVIKpGVHp7B+DisiEhf2ZkWZqDoVBkOUkZ7Q96MPtnuNvOrfd7d7rnUQfGJ7vd/Pe+z2/c88954YwLPAfInkwp9MJZrMZZmdnqR76Mkj/uhO/KwE2hW4GmUwGSUlJIJfL8RvpkCSY3W6HwcFBMH96BxPjFlDEhUC2qhTi4+PxF96MjY3BvZ47MP19GsIV4XBgfwoNqVAo8BdrY03BbDYbtHW2wtTkJLS1dqIrjqracvg58wO2RGyFUydyICYmBr8Rh6hgZLj1Pu6Gtx/ewPWrbehKh7YsD5TbYtmA2eIrSIIJoe/1Kyb/TC6q4GEwGBhNYS7TdbeDWVhYQJc/gip2ix12EzPvobGkHZ3gU1BwErbviIHzZ8shNDQUXR7QeAEgPabT6RizeQCd9YVUj5zfarWiE5gNmI8TMkHU1RWBSnUIjMYX6K4vIyMjkJCQAA0NlWAe+ohuADCgT0ilyspOo1qC9Nx64nk+jSaTsVgsqLjxG6yp6QK2VrNe4bjOQ8LNzc2h8g1nMIOhk/0ko/Im2OECHf/cxQq/s6XPYGSSaGpKQ8VNsMLxPe619pvY8sYrGOkFrTYLVWCkDifkeGRIcs2UXs+x3t7boFTGsuu2ZHQCU1NTQz8uyDqww9gAtq8MRCkj0V3BOvUNIuI2Qu6xklXrSc/j8EHXUAe1pZWo3KDxEHJDarWZqITh6umi2iym9lIV09/fzzgcDup5QkbFwMAAc7lRzxRW5FBPbOW5ZslVwbq6bqzpIUwujt2y+L2pPRkdHaX/V1lTho5wNMVabK2wHIz0LkkvFr2+kLNCgSAdQe4VseF8VW05GBkaLS2HUQmDhBJSJS7IxYkJZzKZmPs93aiWWA7W0lJHh4VQqqvzAj4shfDgYQ9dGwrFczjSteLi4iKMsztffzteXzQ3q0Gtzpd0a39clQbGvueoBOD8RWdjFzQYMZTK39QQgs0WRbfzUkK2JulH09jHTi86/EhM3AMm0zNUGGyS3dpHR09Rgy/t7QchJSUdlbSoj6jgkfEJKn7k5xfD8PAQKgw2Pz8NGRmfqcGX4eFItpcSUUnPzrhYbPGHVM0FDWa326gQQliYTLI3Sr7Yt3uv4OHocKzcTlgxOxVCkMtl2AoOqamp2OKPew6s2B8qhCCXh2MreHS/fIotfrjnoItg8k5PDHpdPbakh7ziq7+iR8Uf1zWt6YXpvwwdiv8fAH8BwL6aSfkaK3sAAAAASUVORK5CYII=";

        //set initial value to closed
        $('.toggle_image').each(function () {
            $(this).attr("src", btnToggleNone);
        });

        // Process each icon with the 'feature_icon' class.
        $('a.feature_icon').each((idx, elt) => {
            const targetRegexValue = $(elt).data('target');
            if (!targetRegexValue) {
                $(elt).addClass('disabled'); // Disable icon if no target exists
                return;
            }

            let foundTag = false;
            const targetRegex = new RegExp(targetRegexValue, 'i');

            // We have a target, treated as a regular expression.
            // We're goint to go through all results (this.md) and if it contains
            // a tag that matches what we're looking for, we'll keep that icon visible.
            search_loop:
            for (let match of this.md) {
                for (let tag of match.tags) {
                    if (targetRegex.exec(tag)) {
                        foundTag = true;        // We have at least one match for this icon
                        break search_loop;
                    }
                }
            }
            if (!foundTag) {
                $(elt).addClass('disabled');
            }
        });

        // Process each confidence image
        $('.confidence_image').each(function () {
            var sourceData = emptyConfImage;
            var confidenceValue = $(this).attr('value').toLowerCase();

            if (!confidenceValue || confidenceValue.length == 0)
                sourceData = emptyConfImage;
            else if (confidenceValue == "high")
                sourceData = highConfImage;
            else if (confidenceValue == "medium")
                sourceData = medConfImage;
            else if (confidenceValue == "low")
                sourceData = lowConfImage;

            $(this).attr("src", sourceData);

        });

        //Toggles display blocks and image; NOTE jquery selectors won't work due to spaces in some group
        //names even using escape() or '[id=value] methods so html DOM methods used and work
        $('.toggle_image').click(function ()
        {
            var btnToggleBlock = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAEEAAAA2CAYAAACY0PQ8AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAAHYYAAB2GAV2iE4EAAACgSURBVGhD7dmxDcIwEEBRhyWYjumYjinCQ0qProIo/zV2aX35GnvbWRd3O9ZLKwJFoAgUgSJQBIpAESgCRaAIFIEiUASKQBEoAkWgCBSBIjB+ct+er2P33/bH/dh9N7oJZwnwMTlr40ARGEWYzNmvTc7aXySNA0WgCBSBIlAEikARKAJFoAgUgSJQBIpAESgCRaAIFIEiUASKQBEowlrrDWP+FV5LruwQAAAAAElFTkSuQmCC";
            var btnToggleNone = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAEEAAAA2CAYAAACY0PQ8AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsQAAA7EAZUrDhsAAADBSURBVGhD7dqxCYBADEDRnEs4ndM5nVPoP7hCxCbNBfG/Ru3kYxCD7UT83DKOv2YEGAFGgBFQ+nZo+zHOIs5tHWfzlT0J9wDd83omxwFGgBFgBBgBRoARYAQYAUaAEWAEGAFGgBGQXqpUfvdnZJY0qSfhKwG6zL06DjACUhEql6FZmXst2za/zWxVZMcBRoARYAQYAUaAEWAEGAFGgBFgBBgBRoARYAQYAWURnlukytWdv/rDcYARYAQYAUaAESLiAh45KVR0Pg9xAAAAAElFTkSuQmCC";

            var groupName = $(this).attr('value');
            var toggleValue1 = document.getElementById(groupName + '-summary').style.display;
            var toggleValue2 = document.getElementById(groupName + '-detailed').style.display;

            document.getElementById(groupName + '-summary').style.display = toggleValue2;
            document.getElementById(groupName + '-detailed').style.display = toggleValue1;

            //update block toggle selection image
            if (document.getElementById(groupName + '-summary').style.display == "none")
                document.getElementById(groupName + '-toggleBtn').src = btnToggleBlock;
            else
                document.getElementById(groupName + '-toggleBtn').src = btnToggleNone;
        });

        // Event handler for visible icons
        $('a.feature_icon').on('click', (e) => {
            const targetRegexValue = $(e.target).closest('a').data('target');
            if (!targetRegexValue) {
                console.log('Error: No target regular expression. This probably indicates a bug.');
                return;
            }
            const targetRegex = new RegExp(targetRegexValue, 'i');
            let identifiedRules = {};
            for (let match of this.md) {
                for (let tag of match.tags) {
                    if (targetRegex.exec(tag)) {
                        identifiedRules[match.ruleName] = this.combineConfidence(identifiedRules[match.ruleName], match.confidence);
                        break;  // Only break out of inner loop, we only need one match per tag set
                    }
                }
            }

            const $tbody = $('#features table tbody');
            $tbody.empty();

            // Now we iterate through all of the rules that relate to this icon
            for (let [rule] of Object.entries(identifiedRules))
            {
                let $tr = $('<tr>');
                $tr.on('click', 'td', { 'obj': this }, this.show_file_listing);
                let $td0 = $('<td>');
                let $td0a = $('<a>');
                $td0a.attr('href', '#');
                $td0a.data('ruleId', rule);
                $td0a.text(rule);
                $td0.append($td0a);
                $tr.append($td0);
                $tbody.append($tr);
            }
        });
    }
}

function SortbyConfidence() {
    $('#divconf').attr('style', 'display:normal;border:none');
    $('#divsev').attr('style', 'display:none');
    $('#divtag').attr('style', 'display:none');
}

function SortbySeverity() {
    $('#divsev').attr('style', 'display:normal;border:none');
    $('#divconf').attr('style', 'display:none');
    $('#divtag').attr('style', 'display:none');
}

function SortbyTags() {
    $('#divtag').attr('style', 'display:normal;border:none');
    $('#divsev').attr('style', 'display:none');
    $('#divconf').attr('style', 'display:none');
}