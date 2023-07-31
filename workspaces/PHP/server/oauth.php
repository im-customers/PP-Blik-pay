<?php
// main.php

require_once 'config.php';

function getAccessToken()
{
  $credentials = base64_encode(CLIENT_ID . ':' . CLIENT_SECRET);

  $url = PAYPAL_API_BASE . '/v1/oauth2/token';
  $data = 'grant_type=client_credentials';

  $headers = [
    'Accept: application/json',
    'Authorization: Basic ' . $credentials,
    'Content-Type: application/x-www-form-urlencoded',
  ];

  $ch = curl_init($url);
  curl_setopt($ch, CURLOPT_POST, true);
  curl_setopt($ch, CURLOPT_POSTFIELDS, $data);
  curl_setopt($ch, CURLOPT_RETURNTRANSFER, true);
  curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

  $response = curl_exec($ch);

  if (curl_errno($ch)) {
    // Handle the error case here
    curl_close($ch);
    return null;
  }

  curl_close($ch);

  $data = json_decode($response, true);
  return $data;
}

// Example usage:
$accessTokenData = getAccessToken();
if ($accessTokenData) {
  $accessToken = $accessTokenData['access_token'];
  echo "Access Token: " . $accessToken;
} else {
  echo "Failed to get the access token.";
}