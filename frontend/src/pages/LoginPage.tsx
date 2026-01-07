import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { LogIn } from 'lucide-react'
import { Button } from '@/components/ui/Button'
import { Input } from '@/components/ui/Input'
import { Alert } from '@/components/ui/Alert'
import { useAuthStore } from '@/store/authStore'
import { authService } from '@/services'

const loginSchema = z.object({
  username: z.string().min(1, 'Username wajib diisi'),
  password: z.string().min(1, 'Password wajib diisi')
})

type LoginFormData = z.infer<typeof loginSchema>

export default function LoginPage() {
  const navigate = useNavigate()
  const { login } = useAuthStore()
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors }
  } = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema)
  })

  const onSubmit = async (data: LoginFormData) => {
    setError(null)
    setIsLoading(true)

    try {
      const response = await authService.login(data)
      
      if (response.success && response.data) {
        login(response.data.user, response.data.token)
        navigate('/dashboard')
      } else {
        setError(response.message || 'Login gagal')
      }
    } catch (err: unknown) {
      const error = err as { response?: { data?: { message?: string } } }
      setError(error.response?.data?.message || 'Terjadi kesalahan. Silakan coba lagi.')
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div>
      <div className="text-center mb-8">
        <h2 className="text-2xl font-bold text-gray-900">Selamat Datang</h2>
        <p className="text-gray-600 mt-2">Silakan masuk ke akun Anda</p>
      </div>

      {error && (
        <Alert variant="error" className="mb-6" onClose={() => setError(null)}>
          {error}
        </Alert>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
        <Input
          label="Username"
          placeholder="Masukkan username"
          error={errors.username?.message}
          {...register('username')}
        />

        <Input
          type="password"
          label="Password"
          placeholder="Masukkan password"
          error={errors.password?.message}
          {...register('password')}
        />

        <Button
          type="submit"
          className="w-full"
          isLoading={isLoading}
          leftIcon={<LogIn className="w-4 h-4" />}
        >
          Masuk
        </Button>
      </form>

      {/* Demo Credentials */}
      <div className="mt-8 p-4 bg-gray-50 rounded-lg">
        <p className="text-xs font-medium text-gray-600 mb-2">Demo Credentials:</p>
        <div className="text-xs text-gray-500 space-y-1">
          <p><span className="font-medium">Pemohon:</span> pemohon1 / password123</p>
          <p><span className="font-medium">PIC L1:</span> pic_l1 / password123</p>
          <p><span className="font-medium">PIC L2:</span> pic_l2 / password123</p>
          <p><span className="font-medium">Driver:</span> driver1 / password123</p>
          <p><span className="font-medium">Admin:</span> admin / password123</p>
        </div>
      </div>
    </div>
  )
}
