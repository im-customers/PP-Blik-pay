require 'rubygems'
require 'dotenv'
Dotenv.load()

NODE_ENV = ENV['NODE_ENV']
CLIENT_ID = ENV['CLIENT_ID']
CLIENT_SECRET = ENV['CLIENT_SECRET']
is_prod = NODE_ENV == "production"

if is_prod
    PAYPAL_API_BASE = "https://api.sample.com"
else
    PAYPAL_API_BASE = "https://api.sandbox.sample.com"
end

puts "CLIENT_ID: #{CLIENT_ID}"
puts "CLIENT_SECRET: #{CLIENT_SECRET}"
puts "PAYPAL_API_BASE: #{PAYPAL_API_BASE}"
