require 'rack/response'
# my_controller.rb
class MyController
    def self.call(env)
        case env["PATH_INFO"]
        when '/' then index
        end
    end
  
    def initialize(env)
      @request = Rack::Request.new(env)
      @response = Rack::Response.new
    end
  
    def self.index
        [200, { 'Content-Type' => 'text/html' }, [File.read("./app/views/index.html")]]
    end
  
    def not_found
      @response.status = 404
      @response.write("Not found")
    end
  end
  