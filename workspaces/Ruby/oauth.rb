require 'httparty'
require 'base64'

PAYPAL_API_BASE = "https://api.sandbox.paypal.com"
CLIENT_ID = "your_client_id"         # Replace with your client ID
CLIENT_SECRET = "your_client_secret" # Replace with your client secret

def get_access_token
  credentials = Base64.strict_encode64("#{CLIENT_ID}:#{CLIENT_SECRET}")

  headers = {
    "Accept" => "application/json",
    "Authorization" => "Basic #{credentials}",
    "Content-Type" => "application/x-www-form-urlencoded"
  }

  response = HTTParty.post(
    "#{PAYPAL_API_BASE}/v1/oauth2/token",
    headers: headers,
    body: { grant_type: 'client_credentials' }
  )

  data = response.parsed_response
  return data
end

