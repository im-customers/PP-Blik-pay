<?php

use GuzzleHttp\Client;

function get_access_token()
{
    $url = PAYPAL_API_BASE . '/v1/oauth2/token';
    $client = new Client();

    // Obtain OAuth 2.0 access token
    $response = $client->post($url, [
        'auth' => [CLIENT_ID, CLIENT_SECRET],
        'form_params' => [
            'grant_type' => 'client_credentials',
        ],
    ]);
    $accessToken = json_decode($response->getBody(), true)['access_token'];
    //$statusCode = $response->getStatusCode();
    return $accessToken;
}