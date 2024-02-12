CREATE PROCEDURE analyzeText @safetyserver nvarchar(100), @safetykey nvarchar(100), @message nvarchar(max)
AS
declare @url nvarchar(4000) = N'https://'+ @safetyserver +'.cognitiveservices.azure.com/contentsafety/text:analyze?api-version=2023-10-01';
declare @headers nvarchar(300) = N'{"Ocp-Apim-Subscription-Key":"'+ @safetykey +'"}';
declare @payload nvarchar(max) = N'{
"text": "'+ @message +'"
}';

declare @ret int, @response nvarchar(max);
exec @ret = sp_invoke_external_rest_endpoint
@url = @url,
@method = 'POST',
@headers = @headers,
@payload = @payload,
@timeout = 230,
@response = @response output;

SELECT JSON_VALUE(D.[value],'$.category') as "Category",
JSON_VALUE(D.[value],'$.severity') as "Severity"
FROM OPENJSON(@response,'$.result.categoriesAnalysis') AS D

GO
