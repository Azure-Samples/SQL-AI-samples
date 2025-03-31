CREATE PROCEDURE check4pii @cogserver nvarchar(100), @cogkey nvarchar(100), @message nvarchar(max)
AS

declare @url nvarchar(4000) = N'https://'+ @cogserver +'.cognitiveservices.azure.com/language/:analyze-text?api-version=2023-04-01';
declare @headers nvarchar(300) = N'{"Ocp-Apim-Subscription-Key":"'+ @cogkey +'"}';
declare @payload nvarchar(max) = N'{
                                        "kind": "PiiEntityRecognition",
                                        "analysisInput":
                                        {
                                            "documents":
                                            [
                                                {
                                                    "id":"1",
                                                    "language": "en",
                                                    "text": "'+ @message +'"
                                                }
                                            ]
                                        }
                                    }';
declare @ret int, @response nvarchar(max);

exec @ret = sp_invoke_external_rest_endpoint
@url = @url,
@method = 'POST',
@headers = @headers,
@payload = @payload,
@timeout = 230,
@response = @response output;

SELECT A.[value] as "Redacted Text"
FROM OPENJSON(@response,'$.result.results.documents') AS D
CROSS APPLY OPENJSON([value]) as A
where A.[key] = 'redactedText'

select JSON_VALUE(B.[value],'$.category') as "PII Category",
JSON_VALUE(B.[value],'$.text') as "PII Value",
CONVERT(FLOAT,JSON_VALUE(B.[value],'$.confidenceScore'))*100 as "Confidence Score"
from OPENJSON(
(
    SELECT A.[value]
    FROM OPENJSON(@response,'$.result.results.documents') AS D
    CROSS APPLY OPENJSON([value]
    ) AS A
    where A.[key] = 'entities'
), '$') AS B

GO
